namespace EditorConfig
{
    public static class PredefinedErrors
    {
        internal class Codes
        {
            public static ErrorCode OnlyRootAllowd = new ErrorCode("EC115", ErrorType.Error);
            public static ErrorCode RootInSection = new ErrorCode("EC108", ErrorType.Warning);
            public static ErrorCode DuplicateSection = new ErrorCode("EC102", ErrorType.Suggestion);
            public static ErrorCode SectionSyntaxError = new ErrorCode("EC109", ErrorType.Error);
            public static ErrorCode GlobbingNoMatch = new ErrorCode("EC103", ErrorType.Suggestion);
            public static ErrorCode DuplicateProperty = new ErrorCode("EC101", ErrorType.Suggestion);
            public static ErrorCode ParentDuplicateProperty = new ErrorCode("EC107", ErrorType.Suggestion);
            public static ErrorCode UnknownValue = new ErrorCode("EC114", ErrorType.Warning);
            public static ErrorCode MissingValue = new ErrorCode("EC105", ErrorType.Error);
            public static ErrorCode UnknownKeyword = new ErrorCode("EC112", ErrorType.Warning);
            public static ErrorCode MissingSeverity = new ErrorCode("EC104", ErrorType.Error);
            public static ErrorCode UnknownSeverity = new ErrorCode("EC113", ErrorType.Warning);
            public static ErrorCode SeverityNotApplicable = new ErrorCode("EC110", ErrorType.Error);
            public static ErrorCode UnknownElement = new ErrorCode("EC111", ErrorType.Error);
        }

        internal class ErrorCode
        {
            public ErrorCode(string code, ErrorType type)
            {
                Code = code;
                Type = type;
            }

            public string Code { get; }
            public ErrorType Type { get; }

            public Error Create(string description)
            {
                return new Error(Code, Type, description);
            }
        }

        //  Root
        public static Error OnlyRootAllowed() =>
            Codes.OnlyRootAllowd.Create(Resources.Text.ValidateOnlyRootAllowed);
        public static Error RootInSection() =>
            Codes.RootInSection.Create(Resources.Text.ValidationRootInSection);

        // Sections
        public static Error DuplicateSection(string sectionText) =>
            Codes.DuplicateSection.Create(string.Format(Resources.Text.ValidationDuplicateSection, sectionText));
        public static Error SectionSyntaxError() => Codes.SectionSyntaxError.Create(Resources.Text.ValidationSectionSyntaxError);
        public static Error GlobbingNoMatch(string sectionText) =>
            Codes.GlobbingNoMatch.Create(string.Format(Resources.Text.ValidationNoMatch, sectionText));

        // Properties
        public static Error DuplicateProperty() =>
            Codes.DuplicateProperty.Create(Resources.Text.ValidationDuplicateProperty);
        public static Error ParentDuplicateProperty(string fileName) =>
            Codes.ParentDuplicateProperty.Create(string.Format(Resources.Text.ValidationParentPropertyDuplicate, fileName));
        public static Error UnknownKeyword(string keywordText) =>
            Codes.UnknownKeyword.Create(string.Format(Resources.Text.ValidateUnknownKeyword, keywordText));

        // Values
        public static Error MissingValue() =>
            Codes.MissingValue.Create(Resources.Text.ValidationMissingPropertyValue);
        public static Error UnknownValue(string valueText, string keywordName) =>
            Codes.UnknownValue.Create(string.Format(Resources.Text.InvalidValue, valueText, keywordName));

        // Severity
        public static Error MissingSeverity() =>
            Codes.MissingSeverity.Create(Resources.Text.ValidationMissingSeverity);
        public static Error UnknownSeverity(string severityText) =>
            Codes.UnknownSeverity.Create(string.Format(Resources.Text.ValidationInvalidSeverity, severityText));
        public static Error SeverityNotApplicable(string keywordName) =>
            Codes.SeverityNotApplicable.Create(string.Format(Resources.Text.ValidationSeverityNotApplicable, keywordName));

        // Misc
        public static Error UnknownElement() =>
            Codes.UnknownElement.Create(Resources.Text.ValidationUnknownElement);
    }
}
