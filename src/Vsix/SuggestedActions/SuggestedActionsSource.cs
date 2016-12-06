using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorConfig
{
    class SuggestedActionsSource : ISuggestedActionsSource
    {
        private IClassifier _classifier;
        private OutliningTagger _tagger;
        private Region _region;

        public SuggestedActionsSource(ITextBuffer buffer, IClassifierAggregatorService classifierService)
        {
            _classifier = classifierService.GetClassifier(buffer);

            buffer.Properties.TryGetProperty(typeof(OutliningTagger), out _tagger);
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                var sections = from c in _classifier.GetClassificationSpans(range)
                               where c.ClassificationType.IsOfType(EditorConfigClassificationTypes.Section)
                               select c;

                foreach (var section in sections)
                {

                    _region = _tagger.Regions.FirstOrDefault(r => r.StartOffset == section.Span.Start);

                    if (_region != null)
                        return true;
                }

                return false;
            });
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            var list = new List<SuggestedActionSet>();

            if (_region != null)
            {
                var span = new Span(_region.StartOffset, _region.EndOffset - _region.StartOffset);

                var deleteSection = new DeleteSectionAction(range.Snapshot.TextBuffer, span);
                yield return new SuggestedActionSet(new[] { deleteSection });
            }
        }

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample provider and doesn't participate in LightBulb telemetry
            telemetryId = Guid.Empty;
            return false;
        }


        public event EventHandler<EventArgs> SuggestedActionsChanged
        {
            add { }
            remove { }
        }
    }
}
