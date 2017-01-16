namespace EditorConfig
{
    public static class PredefinedErrors
    {
        //  Root
        public static Error OnlyRootAllowed() =>
            Create(ErrorCodes.OnlyRootAllowd, Resources.Text.ValidateOnlyRootAllowed);
        public static Error RootInSection() =>
            Create(ErrorCodes.RootInSection, Resources.Text.ValidationRootInSection);

        // Sections
        public static Error DuplicateSection(string sectionText) =>
            Create(ErrorCodes.DuplicateSection, string.Format(Resources.Text.ValidationDuplicateSection, sectionText));
        public static Error SectionSyntaxError() =>
            Create(ErrorCodes.SectionSyntaxError, Resources.Text.ValidationSectionSyntaxError);

        public static Error GlobbingNoMatch(string sectionText) =>
            Create(ErrorCodes.GlobbingNoMatch, string.Format(Resources.Text.ValidationNoMatch, sectionText));

        // Properties
        public static Error DuplicateProperty() =>
            Create(ErrorCodes.DuplicateProperty, Resources.Text.ValidationDuplicateProperty);
        public static Error ParentDuplicateProperty(string fileName) =>
            Create(ErrorCodes.ParentDuplicateProperty, string.Format(Resources.Text.ValidationParentPropertyDuplicate, fileName));
        public static Error UnknownKeyword(string keywordText) =>
            Create(ErrorCodes.UnknownKeyword, string.Format(Resources.Text.ValidateUnknownKeyword, keywordText));

        // Values
        public static Error MissingValue() =>
            Create(ErrorCodes.MissingValue, Resources.Text.ValidationMissingPropertyValue);
        public static Error UnknownValue(string valueText, string keywordName) =>
            Create(ErrorCodes.UnknownValue, string.Format(Resources.Text.InvalidValue, valueText, keywordName));

        // Severity
        public static Error MissingSeverity() =>
            Create(ErrorCodes.MissingSeverity, Resources.Text.ValidationMissingSeverity);
        public static Error UnknownSeverity(string severityText) =>
            Create(ErrorCodes.UnknownSeverity, string.Format(Resources.Text.ValidationInvalidSeverity, severityText));
        public static Error SeverityNotApplicable(string keywordName) =>
            Create(ErrorCodes.SeverityNotApplicable, string.Format(Resources.Text.ValidationSeverityNotApplicable, keywordName));

        // Misc
        public static Error UnknownElement() =>
            Create(ErrorCodes.UnknownElement, Resources.Text.ValidationUnknownElement);

        private static Error Create(ErrorCode errorCode, string description)
        {
            return new Error(errorCode.Code, errorCode.Type, description);
        }
    }
}
