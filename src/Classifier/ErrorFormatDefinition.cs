using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace EditorConfig
{
    public class ErrorFormatDefinition
    {
        public const string Suggestion = Constants.LanguageName + " Message";

        [Export(typeof(ErrorTypeDefinition))]
        [Name(Suggestion)]
        [DisplayName(Suggestion)]
        internal static ErrorTypeDefinition MessageDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [Name(Suggestion)]
        [Order(After = Priority.High)]
        [UserVisible(true)]
        internal class MessageFormat : EditorFormatDefinition
        {
            public MessageFormat()
            {
                DisplayName = Suggestion;
                ForegroundColor = (Color)ColorConverter.ConvertFromString("#80c0c0c0");
            }
        }
    }
}
