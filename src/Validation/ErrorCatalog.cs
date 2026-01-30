using System;
using System.Collections.Generic;

namespace EditorConfig
{
    public static class ErrorCatalog
    {
        private static readonly ValidationOptions _o = EditorConfigPackage.ValidationOptions;
        private static readonly List<Error> _errors = [];
        private static readonly Dictionary<string, Error> _errorLookup = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<Error> All
        {
            get
            {
                return _errors;
            }
        }

        public static Error DuplicateProperty { get; } =
            Create("EC101", ErrorCategory.Warning, Resources.Text.ValidationDuplicateProperty, () => _o.EnableDuplicateProperties);
        public static Error DuplicateSection { get; } =
            Create("EC102", ErrorCategory.Warning, Resources.Text.ValidationDuplicateSection, () => _o.EnableDuplicateSections);
        public static Error GlobbingNoMatch { get; } =
            Create("EC103", ErrorCategory.Suggestion, Resources.Text.ValidationNoMatch, () => _o.EnableGlobbingMatcher);
        public static Error MissingSeverity { get; } =
            Create("EC104", ErrorCategory.Error, Resources.Text.ValidationMissingSeverity);
        public static Error MissingValue { get; } =
            Create("EC105", ErrorCategory.Error, Resources.Text.ValidationMissingPropertyValue);
        public static Error OnlyRootAllowed { get; } =
            Create("EC106", ErrorCategory.Error, Resources.Text.ValidateOnlyRootAllowed);
        public static Error ParentDuplicateProperty { get; } =
            Create("EC107", ErrorCategory.Suggestion, Resources.Text.ValidationParentPropertyDuplicate, () => _o.EnableDuplicateFoundInParent);
        public static Error RootInSection { get; } =
            Create("EC108", ErrorCategory.Warning, Resources.Text.ValidationRootInSection);
        public static Error SectionSyntaxError { get; } =
            Create("EC109", ErrorCategory.Error, Resources.Text.ValidationSectionSyntaxError);
        public static Error SeverityNotApplicable { get; } =
            Create("EC110", ErrorCategory.Error, Resources.Text.ValidationSeverityNotApplicable);
        public static Error UnknownElement { get; } =
            Create("EC111", ErrorCategory.Error, Resources.Text.ValidationUnknownElement);
        public static Error UnknownKeyword { get; } =
            Create("EC112", ErrorCategory.Warning, Resources.Text.ValidateUnknownKeyword, () => _o.EnableUnknownProperties);
        public static Error UnknownSeverity { get; } =
            Create("EC113", ErrorCategory.Warning, Resources.Text.ValidationInvalidSeverity);
        public static Error UnknownValue { get; } =
            Create("EC114", ErrorCategory.Warning, Resources.Text.InvalidValue, () => _o.EnableUnknownValues);
        public static Error TabWidthUnneeded { get; } =
            Create("EC115", ErrorCategory.Suggestion, Resources.Text.ValidationTabWidthUnneeded);
        public static Error IndentSizeUnneeded { get; } =
            Create("EC116", ErrorCategory.Suggestion, Resources.Text.ValidationIndentSizeUnneeded);
        public static Error SpaceInSection { get; } =
            Create("EC117", ErrorCategory.Suggestion, Resources.Text.ValidationSpaceInSection, () => !_o.AllowSpacesInSections);
        public static Error UnknownStyle { get; } =
            Create("EC118", ErrorCategory.Error, Resources.Text.ValidationUnknownStyle);
        public static Error UnusedStyle { get; } =
            Create("EC119", ErrorCategory.Suggestion, Resources.Text.ValidationUnusedStyle);
        public static Error NamingRuleReordered { get; } =
            Create("EC120", ErrorCategory.Warning, Resources.Text.NamingRuleReordered);

        public static bool TryGetErrorCode(string code, out Error errorCode)
        {
            if (code == null)
            {
                errorCode = null;
                return false;
            }

            return _errorLookup.TryGetValue(code, out errorCode);
        }

        private static Error Create(string errorCode, ErrorCategory type, string message)
        {
            return Create(errorCode, type, message, () => true);
        }

        private static Error Create(string errorCode, ErrorCategory type, string message, Func<bool> isSupported)
        {
            var ec = new Error(errorCode, type, message, isSupported);
            _errors.Add(ec);
            _errorLookup[errorCode] = ec;

            return ec;
        }
    }
}
