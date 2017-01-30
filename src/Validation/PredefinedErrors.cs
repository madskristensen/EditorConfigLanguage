namespace EditorConfig
{
    public static class PredefinedErrors
    {
        //  Root
        public static Error OnlyRootAllowed(ParseItem item) =>
            Create(item, ErrorCodes.OnlyRootAllowd, Resources.Text.ValidateOnlyRootAllowed);
        public static Error RootInSection(ParseItem item) =>
            Create(item, ErrorCodes.RootInSection, Resources.Text.ValidationRootInSection);

        // Sections
        public static Error DuplicateSection(ParseItem item) =>
            Create(item, ErrorCodes.DuplicateSection, string.Format(Resources.Text.ValidationDuplicateSection, item.Text));
        public static Error SectionSyntaxError(ParseItem item) =>
            Create(item, ErrorCodes.SectionSyntaxError, Resources.Text.ValidationSectionSyntaxError);

        public static Error GlobbingNoMatch(ParseItem item) =>
            Create(item, ErrorCodes.GlobbingNoMatch, string.Format(Resources.Text.ValidationNoMatch, item.Text));

        // Properties
        public static Error DuplicateProperty(ParseItem item) =>
            Create(item, ErrorCodes.DuplicateProperty, Resources.Text.ValidationDuplicateProperty);
        public static Error ParentDuplicateProperty(ParseItem item, string fileName) =>
            Create(item, ErrorCodes.ParentDuplicateProperty, string.Format(Resources.Text.ValidationParentPropertyDuplicate, fileName));
        public static Error UnknownKeyword(ParseItem item) =>
            Create(item, ErrorCodes.UnknownKeyword, string.Format(Resources.Text.ValidateUnknownKeyword, item.Text));
        public static Error TabWidthUnneeded(ParseItem item) =>
            Create(item, ErrorCodes.TabWidthUnneeded, Resources.Text.ValidationTabWidthUnneeded);
        public static Error IndentSizeUnneeded(ParseItem item) =>
            Create(item, ErrorCodes.IndentSizeUnneeded, Resources.Text.ValidationIndentSizeUnneeded);

        // Values
        public static Error MissingValue(ParseItem item) =>
            Create(item, ErrorCodes.MissingValue, Resources.Text.ValidationMissingPropertyValue);
        public static Error UnknownValue(ParseItem item, string keywordName) =>
            Create(item, ErrorCodes.UnknownValue, string.Format(Resources.Text.InvalidValue, item.Text, keywordName));

        // Severity
        public static Error MissingSeverity(ParseItem item) =>
            Create(item, ErrorCodes.MissingSeverity, Resources.Text.ValidationMissingSeverity);
        public static Error UnknownSeverity(ParseItem item) =>
            Create(item, ErrorCodes.UnknownSeverity, string.Format(Resources.Text.ValidationInvalidSeverity, item.Text));
        public static Error SeverityNotApplicable(ParseItem item, string keywordName) =>
            Create(item, ErrorCodes.SeverityNotApplicable, string.Format(Resources.Text.ValidationSeverityNotApplicable, keywordName));

        // Misc
        public static Error UnknownElement(ParseItem item) =>
            Create(item, ErrorCodes.UnknownElement, Resources.Text.ValidationUnknownElement);

        private static Error Create(ParseItem item, ErrorCode errorCode, string description)
        {
            var error = new Error(item, errorCode.Code, errorCode.Type, description);
            item.AddError(error);
            return error;
        }
    }
}
