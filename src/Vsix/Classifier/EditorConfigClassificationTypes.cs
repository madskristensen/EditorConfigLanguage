using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    public class EditorConfigClassificationTypes
    {
        public const string Section = "EditorConfig Section";
        public const string Comment = PredefinedClassificationTypeNames.Comment;
        public const string Keyword = PredefinedClassificationTypeNames.Identifier;
        public const string Value = PredefinedClassificationTypeNames.Keyword;
        public const string Severity = PredefinedClassificationTypeNames.SymbolDefinition;

        [Export]
        [Name(Section)]
        [BaseDefinition(PredefinedClassificationTypeNames.String)]
        internal static ClassificationTypeDefinition EditorConfigSectionClassification { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = EditorConfigClassificationTypes.Section)]
    [Name(EditorConfigClassificationTypes.Section)]
    [UserVisible(true)]
    internal sealed class SectionFormatDefinition : ClassificationFormatDefinition
    {
        public SectionFormatDefinition()
        {
            IsBold = true;
            DisplayName = EditorConfigClassificationTypes.Section;
        }
    }
}
