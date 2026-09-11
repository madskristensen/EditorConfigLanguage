using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EditorConfig
{
    internal enum NamingEntityKind
    {
        None,
        Rule,
        Symbols,
        Style,
    }

    internal sealed class NamingEntity
    {
        internal NamingEntity(NamingEntityKind kind, string name, ImmutableDictionary<string, ImmutableArray<Property>> members)
        {
            Kind = kind;
            Name = name;
            Members = members;
            Properties = members.Values.SelectMany(properties => properties).ToImmutableArray();
        }

        internal NamingEntityKind Kind { get; }

        internal string Name { get; }

        internal ImmutableDictionary<string, ImmutableArray<Property>> Members { get; }

        internal ImmutableArray<Property> Properties { get; }
    }

    internal sealed class NamingReference
    {
        internal NamingReference(NamingEntityKind targetKind, string name, Property property)
        {
            TargetKind = targetKind;
            Name = name;
            Property = property;
        }

        internal NamingEntityKind TargetKind { get; }

        internal string Name { get; }

        internal Property Property { get; }
    }

    internal sealed class NamingEntityIndex
    {
        private static readonly StringComparer _comparer = StringComparer.OrdinalIgnoreCase;
        private readonly ImmutableDictionary<NamingEntityKind, ImmutableDictionary<string, NamingEntity>> _entities;

        private NamingEntityIndex(
            ImmutableDictionary<NamingEntityKind, ImmutableDictionary<string, NamingEntity>> entities,
            ImmutableArray<NamingReference> references)
        {
            _entities = entities;
            References = references;
        }

        internal static NamingEntityIndex Empty { get; } = new(
            ImmutableDictionary<NamingEntityKind, ImmutableDictionary<string, NamingEntity>>.Empty,
            ImmutableArray<NamingReference>.Empty);

        internal ImmutableArray<NamingReference> References { get; }

        internal static NamingEntityIndex Create(IEnumerable<Property> properties)
        {
            var entities = new Dictionary<NamingEntityKind, Dictionary<string, EntityBuilder>>();
            var references = ImmutableArray.CreateBuilder<NamingReference>();

            foreach (Property property in properties ?? [])
            {
                if (!TryGetNamingProperty(property, out NamingEntityKind kind, out string name, out string member, out Keyword keyword))
                    continue;

                if (!entities.TryGetValue(kind, out Dictionary<string, EntityBuilder> entitiesByName))
                {
                    entitiesByName = new Dictionary<string, EntityBuilder>(_comparer);
                    entities.Add(kind, entitiesByName);
                }

                if (!entitiesByName.TryGetValue(name, out EntityBuilder entity))
                {
                    entity = new EntityBuilder(kind, name);
                    entitiesByName.Add(name, entity);
                }

                entity.Add(member, property);

                if (property.Value != null && TryGetReferenceKind(keyword.ReferenceKind, out NamingEntityKind targetKind))
                {
                    references.Add(new NamingReference(targetKind, property.Value.Text, property));
                }
            }

            ImmutableDictionary<NamingEntityKind, ImmutableDictionary<string, NamingEntity>> immutableEntities = entities
                .ToImmutableDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToImmutableDictionary(
                        entity => entity.Key,
                        entity => entity.Value.ToEntity(),
                        _comparer));

            return new NamingEntityIndex(immutableEntities, references.ToImmutable());
        }

        internal IEnumerable<string> GetNames(NamingEntityKind kind, bool includeReferencedNames = false)
        {
            var names = new HashSet<string>(_comparer);

            if (_entities.TryGetValue(kind, out ImmutableDictionary<string, NamingEntity> entities))
                names.UnionWith(entities.Keys);

            if (includeReferencedNames)
                names.UnionWith(References.Where(reference => reference.TargetKind == kind).Select(reference => reference.Name));

            return names.OrderBy(name => name, _comparer);
        }

        internal bool TryGetEntity(NamingEntityKind kind, string name, out NamingEntity entity)
        {
            entity = null;
            return name != null &&
                   _entities.TryGetValue(kind, out ImmutableDictionary<string, NamingEntity> entities) &&
                   entities.TryGetValue(name, out entity);
        }

        internal IEnumerable<NamingReference> GetReferences(NamingEntityKind kind, string name)
            => References.Where(reference => reference.TargetKind == kind && reference.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        internal static bool TryGetNamingProperty(
            Property property,
            out NamingEntityKind kind,
            out string name,
            out string member,
            out Keyword keyword)
        {
            kind = NamingEntityKind.None;
            name = null;
            member = null;
            keyword = null;

            if (property?.Keyword == null ||
                !SchemaCatalog.TryGetKeyword(property.Keyword.Text, out keyword) ||
                !keyword.TryGetPlaceholderValue(property.Keyword.Text, out name))
            {
                return false;
            }

            kind = GetKind(keyword.Name);
            if (kind == NamingEntityKind.None)
                return false;

            int separator = property.Keyword.Text.LastIndexOf('.');
            if (separator < 0 || separator == property.Keyword.Text.Length - 1)
                return false;

            member = property.Keyword.Text.Substring(separator + 1);
            return true;
        }

        internal static NamingEntityKind GetKind(string keywordName)
        {
            if (keywordName.StartsWith("dotnet_naming_rule.", StringComparison.OrdinalIgnoreCase))
                return NamingEntityKind.Rule;

            if (keywordName.StartsWith("dotnet_naming_symbols.", StringComparison.OrdinalIgnoreCase))
                return NamingEntityKind.Symbols;

            if (keywordName.StartsWith("dotnet_naming_style.", StringComparison.OrdinalIgnoreCase))
                return NamingEntityKind.Style;

            return NamingEntityKind.None;
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

        private sealed class EntityBuilder
        {
            private readonly Dictionary<string, List<Property>> _members = new(_comparer);

            internal EntityBuilder(NamingEntityKind kind, string name)
            {
                Kind = kind;
                Name = name;
            }

            private NamingEntityKind Kind { get; }

            private string Name { get; }

            internal void Add(string member, Property property)
            {
                if (!_members.TryGetValue(member, out List<Property> properties))
                {
                    properties = [];
                    _members.Add(member, properties);
                }

                properties.Add(property);
            }

            internal NamingEntity ToEntity()
                => new(
                    Kind,
                    Name,
                    _members.ToImmutableDictionary(
                        member => member.Key,
                        member => member.Value.ToImmutableArray(),
                        _comparer));
        }
    }
}
