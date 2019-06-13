namespace EditorConfig.Validation.NamingStyles
{
    internal readonly struct NamingRule
    {
        public readonly string Name;
        public readonly SymbolSpecification SymbolSpecification;
        public readonly NamingStyle NamingStyle;
        public readonly ReportDiagnostic EnforcementLevel;

        public NamingRule(string name, SymbolSpecification symbolSpecification, NamingStyle namingStyle, ReportDiagnostic enforcementLevel)
        {
            Name = name;
            SymbolSpecification = symbolSpecification;
            NamingStyle = namingStyle;
            EnforcementLevel = enforcementLevel;
        }
    }
}
