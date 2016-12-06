using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace EditorConfig
{
    internal class EditorConfigQuickInfo : IQuickInfoSource
    {
        private IClassifier _classifier;
        private ITextBuffer _buffer;
        private ErrorTagger _tagger;

        public EditorConfigQuickInfo(ITextBuffer buffer, IClassifierAggregatorService classifierAggregatorService)
        {
            _classifier = classifierAggregatorService.GetClassifier(buffer);
            _buffer = buffer;
            buffer.Properties.TryGetProperty(typeof(ErrorTagger), out _tagger);
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (session == null || qiContent == null || !point.HasValue || point.Value.Position >= point.Value.Snapshot.Length)
                return;

            var line = point.Value.GetContainingLine();
            var lineSpan = new SnapshotSpan(line.Start, line.End);
            var classificationSpans = _classifier.GetClassificationSpans(lineSpan).Where(c => c.ClassificationType.IsOfType(EditorConfigClassificationTypes.Keyword));

            if (!classificationSpans.Any())
                return;

            ShowErrors(qiContent, line);

            var span = classificationSpans.First();
            var keyword = span.Span.GetText()?.Trim();

            var item = CompletionItem.GetCompletionItem(keyword);

            if (item != null)
            {
                qiContent.Add(item.Description);
            }

            applicableToSpan = lineSpan.Snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeNegative);
        }

        private void ShowErrors(IList<object> qiContent, ITextSnapshotLine line)
        {
            var tags = _tagger.GetTags(new NormalizedSnapshotSpanCollection(line.Extent));

            foreach (var tag in tags)
            {
                qiContent.Add(tag.Tag.ToolTipContent);
            }
        }

        public void Dispose()
        {
        }
    }
}
