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
    }
}
