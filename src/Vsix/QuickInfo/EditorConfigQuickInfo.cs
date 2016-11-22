using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace EditorConfig
{
    internal class EditorConfigQuickInfo : IQuickInfoSource
    {
        private IClassifier _classifier;
        private ITextBuffer _buffer;
        private static QuickInfoControl _control;

        public EditorConfigQuickInfo(ITextBuffer buffer, IClassifierAggregatorService classifierAggregatorService, IGlyphService glyphService)
        {
            _classifier = classifierAggregatorService.GetClassifier(buffer);
            _buffer = buffer;

            if (_control == null)
            {
                var image = new Image();
                image.Source = glyphService.GetGlyph(StandardGlyphGroup.GlyphGroupProperty, StandardGlyphItem.GlyphItemPublic);
                _control = new QuickInfoControl(image);
            }
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (!point.HasValue || point.Value.Position >= point.Value.Snapshot.Length)
                return;

            var line = point.Value.GetContainingLine();

            var lineSpan = new SnapshotSpan(line.Start, line.End);
            var classificationSpans = _classifier.GetClassificationSpans(lineSpan).Where(c => c.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Keyword));

            if (!classificationSpans.Any())
                return;

            var span = classificationSpans.First();
            var keyword = span.Span.GetText()?.Trim();

            var item = CompletionItem.GetCompletionItem(keyword);

            if (item != null)
            {
                _control.Keyword.Text = keyword;
                _control.Description.Text = item.Description;
                qiContent.Add(_control);

                applicableToSpan = lineSpan.Snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeNegative);
            }
        }

        private class QuickInfoControl : StackPanel
        {
            public QuickInfoControl(Image image)
            {
                Keyword.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.BrandedUITitleBrushKey);
                Description.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.BrandedUITitleBrushKey);

                image.Margin = new Thickness(0, 0, 10, 0);

                var header = new DockPanel();
                header.Children.Add(image);
                header.Children.Add(Keyword);
                Children.Add(header);

                Children.Add(Description);
            }

            public TextBlock Keyword { get; } = new TextBlock { FontWeight = FontWeights.Bold };
            public TextBlock Description { get; } = new TextBlock { Margin = new Thickness(0, 5, 0, 0), MaxWidth = 500, TextWrapping = TextWrapping.Wrap };
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
