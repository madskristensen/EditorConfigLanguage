using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace EditorConfig
{
    public class EditorConfigClassificationTypes
    {
        public const string Section = Constants.LanguageName + " Section";
        public const string Comment = PredefinedClassificationTypeNames.Comment;
        public const string Keyword = PredefinedClassificationTypeNames.Identifier;
        public const string Value = Constants.LanguageName + " Value";
        public const string Severity = Constants.LanguageName + " Severity";

        [Export, Name(Section)]
        internal static ClassificationTypeDefinition EditorConfigSectionClassification { get; set; }

        [Export, Name(Value)]
        internal static ClassificationTypeDefinition EditorConfigValueClassification { get; set; }

        [Export, Name(Severity)]
        internal static ClassificationTypeDefinition EditorConfigSeverityClassification { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = EditorConfigClassificationTypes.Section)]
    [Name(EditorConfigClassificationTypes.Section)]
    [UserVisible(true)]
    internal sealed class SectionFormatDefinition : ClassificationFormatDefinition
    {
        public SectionFormatDefinition()
        {
            ForegroundColor = (Color)ColorConverter.ConvertFromString("#00cb4b16"); // orange
            IsBold = true;
            DisplayName = EditorConfigClassificationTypes.Section;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = EditorConfigClassificationTypes.Value)]
    [Name(EditorConfigClassificationTypes.Value)]
    [UserVisible(true)]
    internal sealed class ValueFormatDefinition : ClassificationFormatDefinition
    {
        public ValueFormatDefinition()
        {
            ForegroundColor = (Color)ColorConverter.ConvertFromString("#00268bd2"); // blue
            DisplayName = EditorConfigClassificationTypes.Value;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = EditorConfigClassificationTypes.Severity)]
    [Name(EditorConfigClassificationTypes.Severity)]
    [UserVisible(true)]
    internal sealed class SeverityFormatDefinition : ClassificationFormatDefinition
    {
        public SeverityFormatDefinition()
        {
            ForegroundColor = (Color)ColorConverter.ConvertFromString("#002aa198"); // cyan
            DisplayName = EditorConfigClassificationTypes.Severity;
        }
    }
}
