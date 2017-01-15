using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    public class Error : ITooltip
    {
        //public Error(int errorCode, string description, ErrorType errorType)
        //{
        //    ErrorCode = errorCode;
        //    Description = description;
        //    ErrorType = errorType;
        //}

        public Error(string errorCode, ErrorType errorType, string description)
        {
            ErrorCode = errorCode;
            ErrorType = errorType;
            Description = description;
        }

        public string ErrorCode { get; set; }
        public ErrorType ErrorType { get; set; } = ErrorType.Warning;
        public string Description { get; set; }

        public ImageMoniker Moniker
        {
            get
            {
                switch (ErrorType)
                {
                    case ErrorType.Error:
                        return KnownMonikers.StatusError;
                    case ErrorType.Warning:
                        return KnownMonikers.StatusWarning;
                }

                return KnownMonikers.StatusInformation;
            }
        }

        public string Name
        {
            get
            {
                return ErrorType.ToString();
            }
        }

        public bool IsSupported => true;
    }
}
