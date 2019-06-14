using System.Collections.Immutable;

namespace EditorConfig.Validation.NamingStyles
{
    internal readonly struct NamingStyleRules
    {
        public NamingStyleRules(ImmutableArray<NamingRule> namingRules)
        {
            NamingRules = namingRules;
        }

        public ImmutableArray<NamingRule> NamingRules { get; }
    }
}
