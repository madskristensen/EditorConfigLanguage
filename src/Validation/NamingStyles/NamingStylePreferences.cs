using System;
using System.Collections.Immutable;
using System.Linq;

namespace EditorConfig.Validation.NamingStyles
{
    internal class NamingStylePreferences
    {
        public readonly ImmutableArray<SymbolSpecification> SymbolSpecifications;
        public readonly ImmutableArray<NamingStyle> NamingStyles;
        public readonly ImmutableArray<SerializableNamingRule> NamingRules;

        private readonly Lazy<NamingStyleRules> _lazyRules;

        internal NamingStylePreferences(
            ImmutableArray<SymbolSpecification> symbolSpecifications,
            ImmutableArray<NamingStyle> namingStyles,
            ImmutableArray<SerializableNamingRule> namingRules)
        {
            SymbolSpecifications = symbolSpecifications;
            NamingStyles = namingStyles;
            NamingRules = namingRules;

            _lazyRules = new Lazy<NamingStyleRules>(CreateRules, isThreadSafe: true);
        }

        public NamingStyleRules Rules => _lazyRules.Value;

        internal NamingStyle GetNamingStyle(Guid namingStyleID)
            => NamingStyles.Single(s => s.ID == namingStyleID);

        internal SymbolSpecification GetSymbolSpecification(Guid symbolSpecificationID)
            => SymbolSpecifications.Single(s => s.ID == symbolSpecificationID);

        private NamingStyleRules CreateRules()
            => new NamingStyleRules(NamingRules.Select(r => r.GetRule(this)).ToImmutableArray());
    }
}
