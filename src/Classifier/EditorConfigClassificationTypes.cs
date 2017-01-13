using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace EditorConfig
{
    public class EditorConfigClassificationTypes
    {
        public const string Section = PredefinedClassificationTypeNames.String;
        public const string Comment = PredefinedClassificationTypeNames.Comment;
        public const string Keyword = PredefinedClassificationTypeNames.Identifier;
        public const string Value = PredefinedClassificationTypeNames.Keyword;
        public const string Severity = PredefinedClassificationTypeNames.SymbolDefinition;
        public const string Duplicate = PredefinedClassificationTypeNames.ExcludedCode;
        public const string NoMatches = Constants.LanguageName + " No Matches";

        [Export]
        [Name(NoMatches)]
        [BaseDefinition(Duplicate)]
        internal static ClassificationTypeDefinition EditorConfigNoMatchesClassification { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = EditorConfigClassificationTypes.NoMatches)]
    [Name(EditorConfigClassificationTypes.NoMatches)]
    [UserVisible(true)]
    internal sealed class NoMatchesFormatDefinition : ClassificationFormatDefinition
    {
        public NoMatchesFormatDefinition()
        {
            DisplayName = EditorConfigClassificationTypes.Duplicate;
            IsBold = true;
        }
    }
}
