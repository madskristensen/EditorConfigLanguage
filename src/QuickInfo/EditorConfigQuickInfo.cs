using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace EditorConfig
{
    internal class EditorConfigQuickInfo(ITextBuffer buffer) : IAsyncQuickInfoSource
    {
        private readonly EditorConfigDocument _document = EditorConfigDocument.FromTextBuffer(buffer);

        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            SnapshotPoint? point = session.GetTriggerPoint(buffer.CurrentSnapshot);

            if (!point.HasValue || point.Value.Position >= point.Value.Snapshot.Length)
                return Task.FromResult<QuickInfoItem>(null);

            ParseItem item = _document.ItemAtPosition(point.Value);

            if (item == null)
                return Task.FromResult<QuickInfoItem>(null);

            ITrackingSpan applicableToSpan = point.Value.Snapshot.CreateTrackingSpan(item.Span, SpanTrackingMode.EdgeNegative);

            // Handle errors first
            if (item.Errors.Any())
            {
                DisplayError error = item.Errors.First();
                ContainerElement element = QuickInfoBuilder.BuildTooltip(error);
                return Task.FromResult(new QuickInfoItem(applicableToSpan, element));
            }

            Property property = _document.PropertyAtPosition(point.Value);
            SchemaCatalog.TryGetKeyword(property?.Keyword?.Text, out Keyword keyword);

            // Keyword
            if (keyword != null && item.ItemType == ItemType.Keyword)
            {
                ContainerElement element = QuickInfoBuilder.BuildTooltip(keyword);
                return Task.FromResult(new QuickInfoItem(applicableToSpan, element));
            }

            // Value
            if (keyword != null && item.ItemType == ItemType.Value)
            {
                Value value = keyword.Values.FirstOrDefault(v => v.Name.Is(item.Text));

                if (value != null && !string.IsNullOrEmpty(value.Description))
                {
                    ContainerElement element = QuickInfoBuilder.BuildTooltip(value);
                    return Task.FromResult(new QuickInfoItem(applicableToSpan, element));
                }
            }

            // Severity
            if (item.ItemType == ItemType.Severity && SchemaCatalog.TryGetSeverity(item.Text, out Severity severity))
            {
                ContainerElement element = QuickInfoBuilder.BuildTooltip(severity);
                return Task.FromResult(new QuickInfoItem(applicableToSpan, element));
            }

            // Suppression
            if (item.ItemType == ItemType.Suppression && ErrorCatalog.TryGetErrorCode(item.Text, out Error code))
            {
                ContainerElement element = QuickInfoBuilder.BuildTooltip(code);
                return Task.FromResult(new QuickInfoItem(applicableToSpan, element));
            }

            return Task.FromResult<QuickInfoItem>(null);
        }

        public void Dispose()
        {
        }
    }
}
