using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    public class EditorConfigClassificationTypes
    {
        public const string Header = "EditorConfig Header";
        public const string Comment = PredefinedClassificationTypeNames.Comment;
        public const string Keyword = PredefinedClassificationTypeNames.Identifier;
        public const string Value = PredefinedClassificationTypeNames.Keyword;
        public const string Severity = PredefinedClassificationTypeNames.SymbolDefinition;

        [Export]
        [Name(Header)]
        [BaseDefinition(PredefinedClassificationTypeNames.String)]
        internal static ClassificationTypeDefinition EditorConfigHeaderClassification { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = EditorConfigClassificationTypes.Header)]
    [Name(EditorConfigClassificationTypes.Header)]
    [UserVisible(true)]
    internal sealed class HeaderFormatDefinition : ClassificationFormatDefinition
    {
        public HeaderFormatDefinition()
        {
            IsBold = true;
            DisplayName = EditorConfigClassificationTypes.Header;
        }
    }
}
