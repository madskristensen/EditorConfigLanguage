namespace EditorConfig
{
    public class ErrorCodes
    {
        public static ErrorCode DuplicateProperty = new ErrorCode("EC101", ErrorType.Warning);
        public static ErrorCode DuplicateSection = new ErrorCode("EC102", ErrorType.Warning);
        public static ErrorCode GlobbingNoMatch = new ErrorCode("EC103", ErrorType.Suggestion);
        public static ErrorCode MissingSeverity = new ErrorCode("EC104", ErrorType.Error);
        public static ErrorCode MissingValue = new ErrorCode("EC105", ErrorType.Error);
        public static ErrorCode OnlyRootAllowd = new ErrorCode("EC106", ErrorType.Error);
        public static ErrorCode ParentDuplicateProperty = new ErrorCode("EC107", ErrorType.Suggestion);
        public static ErrorCode RootInSection = new ErrorCode("EC108", ErrorType.Warning);
        public static ErrorCode SectionSyntaxError = new ErrorCode("EC109", ErrorType.Error);
        public static ErrorCode SeverityNotApplicable = new ErrorCode("EC110", ErrorType.Error);
        public static ErrorCode UnknownElement = new ErrorCode("EC111", ErrorType.Error);
        public static ErrorCode UnknownKeyword = new ErrorCode("EC112", ErrorType.Warning);
        public static ErrorCode UnknownSeverity = new ErrorCode("EC113", ErrorType.Warning);
        public static ErrorCode UnknownValue = new ErrorCode("EC114", ErrorType.Warning);
        public static ErrorCode TabWidthUnneeded = new ErrorCode("EC115", ErrorType.Suggestion);
        public static ErrorCode IndentSizeUnneeded = new ErrorCode("EC116", ErrorType.Suggestion);
    }

    public struct ErrorCode
    {
        public ErrorCode(string code, ErrorType type)
        {
            Code = code;
            Type = type;
        }

        public string Code { get; }
        public ErrorType Type { get; }
    }
}
