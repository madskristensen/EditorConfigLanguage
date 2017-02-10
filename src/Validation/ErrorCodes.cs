using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    public class ErrorCodes
    {
        public static ErrorCode DuplicateProperty = new ErrorCode("EC101", ErrorType.Warning, nameof(DuplicateProperty));
        public static ErrorCode DuplicateSection = new ErrorCode("EC102", ErrorType.Warning, nameof(DuplicateSection));
        public static ErrorCode GlobbingNoMatch = new ErrorCode("EC103", ErrorType.Suggestion, nameof(GlobbingNoMatch));
        public static ErrorCode MissingSeverity = new ErrorCode("EC104", ErrorType.Error, nameof(MissingSeverity));
        public static ErrorCode MissingValue = new ErrorCode("EC105", ErrorType.Error, nameof(MissingValue));
        public static ErrorCode OnlyRootAllowd = new ErrorCode("EC106", ErrorType.Error, nameof(OnlyRootAllowd));
        public static ErrorCode ParentDuplicateProperty = new ErrorCode("EC107", ErrorType.Suggestion, nameof(ParentDuplicateProperty));
        public static ErrorCode RootInSection = new ErrorCode("EC108", ErrorType.Warning, nameof(RootInSection));
        public static ErrorCode SectionSyntaxError = new ErrorCode("EC109", ErrorType.Error, nameof(SectionSyntaxError));
        public static ErrorCode SeverityNotApplicable = new ErrorCode("EC110", ErrorType.Error, nameof(SeverityNotApplicable));
        public static ErrorCode UnknownElement = new ErrorCode("EC111", ErrorType.Error, nameof(UnknownElement));
        public static ErrorCode UnknownKeyword = new ErrorCode("EC112", ErrorType.Warning, nameof(UnknownKeyword));
        public static ErrorCode UnknownSeverity = new ErrorCode("EC113", ErrorType.Warning, nameof(UnknownSeverity));
        public static ErrorCode UnknownValue = new ErrorCode("EC114", ErrorType.Warning, nameof(UnknownValue));
        public static ErrorCode TabWidthUnneeded = new ErrorCode("EC115", ErrorType.Suggestion, nameof(TabWidthUnneeded));
        public static ErrorCode IndentSizeUnneeded = new ErrorCode("EC116", ErrorType.Suggestion, nameof(IndentSizeUnneeded));
        public static ErrorCode SpaceInSection = new ErrorCode("EC117", ErrorType.Suggestion, nameof(SpaceInSection));

        public static IReadOnlyList<ErrorCode> All = new List<ErrorCode>
        {
            DuplicateProperty,
            DuplicateSection,
            GlobbingNoMatch,
            MissingSeverity,
            MissingValue,
            OnlyRootAllowd,
            ParentDuplicateProperty,
            RootInSection,
            SectionSyntaxError,
            SeverityNotApplicable,
            UnknownElement,
            UnknownKeyword,
            UnknownSeverity,
            UnknownValue,
            TabWidthUnneeded,
            IndentSizeUnneeded,
            SpaceInSection
        };

        public static bool TryGetErrorCode(string code, out ErrorCode errorCode)
        {
            errorCode = All.FirstOrDefault(c => c.Code.Equals(code));
            return errorCode != null;
        }
    }

    public class ErrorCode : ITooltip
    {
        private string _description;

        public ErrorCode(string code, ErrorType type, string description)
        {
            Code = code;
            Type = type;
            _description = description;
        }

        public string Code { get; }
        public ErrorType Type { get; }

        string ITooltip.Name => Code;

        string ITooltip.Description => $"{Prettify(_description)}\r\nCategory: {Type}";

        ImageMoniker ITooltip.Moniker
        {
            get
            {
                switch (Type)
                {
                    case ErrorType.Error:
                        return KnownMonikers.StatusError;
                    case ErrorType.Warning:
                        return KnownMonikers.StatusWarning;
                    default:
                        return KnownMonikers.StatusInformation;
                }
            }
        }

        bool ITooltip.IsSupported => true;

        private string Prettify(string text)
        {
            string output = "";

            foreach (char letter in text)
            {
                if (char.IsUpper(letter) && output.Length > 0)
                    output += " " + letter;
                else
                    output += letter;
            }

            return output;
        }
    }
}
