using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    internal enum NamingCompletionKind
    {
        None,
        EntityName,
        Member,
        ReferenceValue,
    }

    internal sealed class NamingCompletionItem : ITooltip
    {
        internal NamingCompletionItem(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }

        public string Description { get; }

        public ImageMoniker Moniker => KnownMonikers.Property;

        public bool IsSupported => true;
    }

    internal sealed class NamingCompletionContext
    {
        internal static NamingCompletionContext Empty { get; } = new(NamingCompletionKind.None, 0, []);

        internal NamingCompletionContext(NamingCompletionKind kind, int spanStart, IReadOnlyList<NamingCompletionItem> items)
        {
            Kind = kind;
            SpanStart = spanStart;
            Items = items;
        }

        internal NamingCompletionKind Kind { get; }

        internal int SpanStart { get; }

        internal IReadOnlyList<NamingCompletionItem> Items { get; }
    }

    internal static class NamingCompletionService
    {
        private static readonly StringComparer _comparer = StringComparer.OrdinalIgnoreCase;

        internal static NamingCompletionContext GetContext(
            string lineText,
            int caretPosition,
            IEnumerable<NamingEntityIndex> indexes)
        {
            if (lineText == null || caretPosition < 0 || caretPosition > lineText.Length)
                return NamingCompletionContext.Empty;

            NamingEntityIndex[] availableIndexes = indexes?.ToArray() ?? [];
            int equals = lineText.IndexOf('=');

            if (equals >= 0 && caretPosition > equals)
                return GetReferenceValueContext(lineText, equals, caretPosition, availableIndexes);

            return GetKeyContext(lineText, caretPosition, availableIndexes);
        }

        private static NamingCompletionContext GetKeyContext(
            string lineText,
            int caretPosition,
            IReadOnlyList<NamingEntityIndex> indexes)
        {
            int keyStart = 0;
            while (keyStart < caretPosition && char.IsWhiteSpace(lineText[keyStart]))
                keyStart++;

            string key = lineText.Substring(keyStart, caretPosition - keyStart);
            if (!TryGetFamily(key, out NamingEntityKind kind, out string prefix))
                return NamingCompletionContext.Empty;

            string remainder = key.Substring(prefix.Length);
            int memberSeparator = remainder.IndexOf('.');

            if (memberSeparator < 0)
            {
                var declaredNames = new HashSet<string>(_comparer);
                var allNames = new HashSet<string>(_comparer);

                foreach (NamingEntityIndex index in indexes)
                {
                    declaredNames.UnionWith(index.GetNames(kind));
                    allNames.UnionWith(index.GetNames(kind, includeReferencedNames: true));
                }

                NamingCompletionItem[] names = allNames
                    .OrderBy(name => name, _comparer)
                    .Select(name => new NamingCompletionItem(
                        name,
                        declaredNames.Contains(name)
                            ? $"{GetDisplayName(kind)} declared in the applicable configuration."
                            : $"{GetDisplayName(kind)} is referenced but not yet declared."))
                    .ToArray();

                return new NamingCompletionContext(
                    NamingCompletionKind.EntityName,
                    keyStart + prefix.Length,
                    names);
            }

            string entityName = remainder.Substring(0, memberSeparator);
            int memberStart = keyStart + prefix.Length + memberSeparator + 1;
            var usedMembers = new HashSet<string>(_comparer);

            foreach (NamingEntityIndex index in indexes)
            {
                if (index.TryGetEntity(kind, entityName, out NamingEntity entity))
                    usedMembers.UnionWith(entity.Members.Keys);
            }

            NamingCompletionItem[] members = SchemaCatalog.VisibleKeywords
                .Where(keyword => NamingEntityIndex.GetKind(keyword.Name) == kind)
                .Select(keyword => new
                {
                    Keyword = keyword,
                    Member = keyword.Name.Substring(keyword.Name.LastIndexOf('.') + 1),
                })
                .Where(item => !usedMembers.Contains(item.Member))
                .GroupBy(item => item.Member, _comparer)
                .Select(group => group.First())
                .OrderBy(item => item.Member, _comparer)
                .Select(item => new NamingCompletionItem(item.Member, item.Keyword.Description))
                .ToArray();

            return new NamingCompletionContext(NamingCompletionKind.Member, memberStart, members);
        }

        private static NamingCompletionContext GetReferenceValueContext(
            string lineText,
            int equals,
            int caretPosition,
            IReadOnlyList<NamingEntityIndex> indexes)
        {
            string keywordText = lineText.Substring(0, equals).Trim();
            if (!SchemaCatalog.TryGetKeyword(keywordText, out Keyword keyword) ||
                !TryGetReferenceKind(keyword.ReferenceKind, out NamingEntityKind targetKind))
            {
                return NamingCompletionContext.Empty;
            }

            int valueStart = equals + 1;
            while (valueStart < caretPosition && char.IsWhiteSpace(lineText[valueStart]))
                valueStart++;

            var names = new HashSet<string>(_comparer);
            foreach (NamingEntityIndex index in indexes)
                names.UnionWith(index.GetNames(targetKind));

            NamingCompletionItem[] items = names
                .OrderBy(name => name, _comparer)
                .Select(name => new NamingCompletionItem(name, $"{GetDisplayName(targetKind)} declared in the applicable configuration."))
                .ToArray();

            return new NamingCompletionContext(NamingCompletionKind.ReferenceValue, valueStart, items);
        }

        private static bool TryGetFamily(string key, out NamingEntityKind kind, out string prefix)
        {
            foreach ((string candidate, NamingEntityKind candidateKind) in new[]
            {
                ("dotnet_naming_rule.", NamingEntityKind.Rule),
                ("dotnet_naming_symbols.", NamingEntityKind.Symbols),
                ("dotnet_naming_style.", NamingEntityKind.Style),
            })
            {
                if (key.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    kind = candidateKind;
                    prefix = candidate;
                    return true;
                }
            }

            kind = NamingEntityKind.None;
            prefix = null;
            return false;
        }

        private static bool TryGetReferenceKind(string referenceKind, out NamingEntityKind kind)
        {
            if (referenceKind.Is("naming_symbols"))
            {
                kind = NamingEntityKind.Symbols;
                return true;
            }

            if (referenceKind.Is("naming_style"))
            {
                kind = NamingEntityKind.Style;
                return true;
            }

            kind = NamingEntityKind.None;
            return false;
        }

        private static string GetDisplayName(NamingEntityKind kind)
        {
            return kind switch
            {
                NamingEntityKind.Rule => "Naming rule",
                NamingEntityKind.Symbols => "Symbol group",
                NamingEntityKind.Style => "Naming style",
                _ => "Naming entity",
            };
        }
    }
}
