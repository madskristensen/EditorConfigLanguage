using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace EditorConfig
{
    public class EditorConfigClassificationTypes
    {
        public const string Section = PredefinedClassificationTypeNames.String;
        public const string Comment = PredefinedClassificationTypeNames.Comment;
        public const string Keyword = PredefinedClassificationTypeNames.Identifier;
        public const string Value = PredefinedClassificationTypeNames.Keyword;
        public const string Severity = PredefinedClassificationTypeNames.SymbolDefinition;
        public const string Duplicate = Constants.LanguageName + " Duplicate";

        [Export, Name(Duplicate)]
        internal static ClassificationTypeDefinition EditorConfigDuplicateClassification { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = EditorConfigClassificationTypes.Duplicate)]
    [Name(EditorConfigClassificationTypes.Duplicate)]
    [UserVisible(true)]
    internal sealed class DuplicateFormatDefinition : ClassificationFormatDefinition
    {
        public DuplicateFormatDefinition()
        {
            ForegroundColor = (Color)ColorConverter.ConvertFromString("#00839496"); // base0
            DisplayName = EditorConfigClassificationTypes.Duplicate;
        }
    }
}
