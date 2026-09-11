using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    internal sealed class NamingSymbol
    {
        internal NamingSymbol(NamingEntityKind kind, string name, Span span, Property property, bool isDeclaration)
        {
            Kind = kind;
            Name = name;
            Span = span;
            Property = property;
            IsDeclaration = isDeclaration;
        }

        internal NamingEntityKind Kind { get; }

        internal string Name { get; }

        internal Span Span { get; }

        internal Property Property { get; }

        internal bool IsDeclaration { get; }
    }

    internal sealed class NamingReferenceResult
    {
        internal NamingReferenceResult(NamingSymbol symbol, int line, int column, string lineText)
        {
            Symbol = symbol;
            Line = line;
            Column = column;
            LineText = lineText;
        }

        internal NamingSymbol Symbol { get; }

        internal int Line { get; }

        internal int Column { get; }

        internal string LineText { get; }
    }

    internal static class NamingSymbolService
    {
        internal static bool TryGetSymbolAtPosition(EditorConfigDocument document, int position, out NamingSymbol symbol)
        {
            symbol = null;
            Property property = document.PropertyAtPosition(position + 1);
            if (property == null)
                return false;

            if (property.Value != null &&
                property.Value.Span.Contains(position) &&
                SchemaCatalog.TryGetKeyword(property.Keyword.Text, out Keyword referenceKeyword) &&
                NamingEntityIndex.TryGetReferenceKind(referenceKeyword.ReferenceKind, out NamingEntityKind targetKind))
            {
                symbol = new NamingSymbol(targetKind, property.Value.Text, property.Value.Span, property, isDeclaration: false);
                return true;
            }

            if (!property.Keyword.Span.Contains(position) ||
                !NamingEntityIndex.TryGetNamingProperty(property, out NamingEntityKind kind, out string name, out _, out _))
            {
                return false;
            }

            int nameStart = property.Keyword.Text.IndexOf('.') + 1;
            int nameEnd = property.Keyword.Text.IndexOf('.', nameStart);
            if (nameStart <= 0 || nameEnd <= nameStart)
                return false;

            var nameSpan = new Span(property.Keyword.Span.Start + nameStart, nameEnd - nameStart);
            if (!nameSpan.Contains(position))
                return false;

            symbol = new NamingSymbol(kind, name, nameSpan, property, isDeclaration: true);
            return true;
        }

        internal static IReadOnlyList<NamingSymbol> FindOccurrences(EditorConfigDocument document, NamingEntityKind kind, string name)
        {
            var occurrences = new List<NamingSymbol>();

            if (document.NamingEntities.TryGetEntity(kind, name, out NamingEntity entity))
            {
                foreach (Property property in entity.Properties)
                {
                    int nameStart = property.Keyword.Text.IndexOf('.') + 1;
                    int nameEnd = property.Keyword.Text.IndexOf('.', nameStart);
                    if (nameStart <= 0 || nameEnd <= nameStart)
                        continue;

                    occurrences.Add(new NamingSymbol(
                        kind,
                        name,
                        new Span(property.Keyword.Span.Start + nameStart, nameEnd - nameStart),
                        property,
                        isDeclaration: true));
                }
            }

            occurrences.AddRange(document.NamingEntities
                .GetReferences(kind, name)
                .Select(reference => new NamingSymbol(
                    kind,
                    name,
                    reference.Property.Value.Span,
                    reference.Property,
                    isDeclaration: false)));

            return occurrences
                .GroupBy(occurrence => occurrence.Span)
                .Select(group => group.First())
                .OrderBy(occurrence => occurrence.Span.Start)
                .ToArray();
        }

        internal static bool TryGetDefinition(EditorConfigDocument document, NamingSymbol symbol, out NamingSymbol definition)
        {
            definition = FindOccurrences(document, symbol.Kind, symbol.Name)
                .FirstOrDefault(occurrence => occurrence.IsDeclaration);
            return definition != null;
        }

        internal static IReadOnlyList<NamingReferenceResult> FindReferenceResults(
            EditorConfigDocument document,
            ITextSnapshot snapshot,
            NamingEntityKind kind,
            string name)
        {
            return FindOccurrences(document, kind, name)
                .Select(symbol =>
                {
                    ITextSnapshotLine line = snapshot.GetLineFromPosition(symbol.Span.Start);
                    return new NamingReferenceResult(
                        symbol,
                        line.LineNumber,
                        symbol.Span.Start - line.Start.Position,
                        line.GetText());
                })
                .ToArray();
        }

        internal static bool TryGetRenameSpans(
            EditorConfigDocument document,
            NamingSymbol symbol,
            string newName,
            out IReadOnlyList<Span> spans,
            out string error)
        {
            spans = [];
            error = null;

            if (string.IsNullOrWhiteSpace(newName))
            {
                error = "The name cannot be empty.";
                return false;
            }

            if (newName.Any(character => !char.IsLetterOrDigit(character) && character != '_' && character != '-'))
            {
                error = "The name can contain only letters, digits, underscores, and hyphens.";
                return false;
            }

            if (!TryGetDefinition(document, symbol, out NamingSymbol definition))
            {
                error = "The declaration could not be found.";
                return false;
            }

            int relativeStart = definition.Span.Start - definition.Property.Keyword.Span.Start;
            string candidate = definition.Property.Keyword.Text
                .Remove(relativeStart, definition.Span.Length)
                .Insert(relativeStart, newName);

            if (!SchemaCatalog.TryGetKeyword(candidate, out Keyword keyword) ||
                NamingEntityIndex.GetKind(keyword.Name) != symbol.Kind ||
                !keyword.TryGetPlaceholderValue(candidate, out string parsedName) ||
                !parsedName.Equals(newName, StringComparison.Ordinal))
            {
                error = "The name is not valid in an EditorConfig property.";
                return false;
            }

            if (!symbol.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                document.NamingEntities.TryGetEntity(symbol.Kind, newName, out _))
            {
                error = $"A {symbol.Kind.ToString().ToLowerInvariant()} named \"{newName}\" already exists.";
                return false;
            }

            spans = FindOccurrences(document, symbol.Kind, symbol.Name)
                .Select(occurrence => occurrence.Span)
                .Distinct()
                .OrderByDescending(span => span.Start)
                .ToArray();
            return spans.Count > 0;
        }
    }
}
