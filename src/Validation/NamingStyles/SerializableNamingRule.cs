using System;

namespace EditorConfig.Validation.NamingStyles
{
    internal sealed class SerializableNamingRule
    {
        public string Name;
        public Guid SymbolSpecificationID;
        public Guid NamingStyleID;
        public ReportDiagnostic EnforcementLevel;

        public NamingRule GetRule(NamingStylePreferences info)
        {
            return new NamingRule(
                Name,
                info.GetSymbolSpecification(SymbolSpecificationID),
                info.GetNamingStyle(NamingStyleID),
                EnforcementLevel);
        }
    }
}
