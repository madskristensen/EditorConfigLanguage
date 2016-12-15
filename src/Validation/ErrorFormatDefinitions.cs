using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace EditorConfig
{
    public class ErrorFormatDefinitions
    {
        public const string Error = Constants.LanguageName + " Error";
        public const string Warning = Constants.LanguageName + " Warning";
        public const string Message = Constants.LanguageName + " Message";

        public static string GetErrorType(ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.Error:
                    return Error;
                case ErrorType.Warning:
                    return Warning;
            }

            return Message;
        }

        // Error
        [Export(typeof(ErrorTypeDefinition))]
        [Name(Error)]
        [DisplayName(Error)]
        internal static ErrorTypeDefinition ErrorDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [Name(Error)]
        [Order(After = Priority.High)]
        [UserVisible(true)]
        internal class ErrorFormat : EditorFormatDefinition
        {
            public ErrorFormat()
            {
                DisplayName = Error;
                ForegroundColor = (Color)ColorConverter.ConvertFromString("#00dc322f");

            }
        }

        // Warning
        [Export(typeof(ErrorTypeDefinition))]
        [Name(Warning)]
        [DisplayName(Warning)]
        internal static ErrorTypeDefinition WarningDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [Name(Warning)]
        [Order(After = Priority.High)]
        [UserVisible(true)]
        internal class WarningFormat : EditorFormatDefinition
        {
            public WarningFormat()
            {
                DisplayName = Error;
                ForegroundColor = (Color)ColorConverter.ConvertFromString("#00dc322f");

            }
        }

        // Message
        [Export(typeof(ErrorTypeDefinition))]
        [Name(Message)]
        [DisplayName(Message)]
        internal static ErrorTypeDefinition MessageDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [Name(Message)]
        [Order(After = Priority.High)]
        [UserVisible(true)]
        internal class MessageFormat : EditorFormatDefinition
        {
            public MessageFormat()
            {
                DisplayName = Error;
                ForegroundColor = (Color)ColorConverter.ConvertFromString("#00268bd2");
            }
        }
    }
}
