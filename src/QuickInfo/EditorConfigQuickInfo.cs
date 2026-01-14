using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EditorConfig
{
    internal class EditorConfigQuickInfo : IAsyncQuickInfoSource
    {
        private ITextBuffer _buffer;
        private EditorConfigDocument _document;

        public EditorConfigQuickInfo(ITextBuffer buffer)
        {
            _buffer = buffer;
            _document = EditorConfigDocument.FromTextBuffer(buffer);
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (session == null || !point.HasValue || point.Value.Position >= point.Value.Snapshot.Length)
                return null;

            ParseItem item = _document.ItemAtPosition(point.Value);

            if (item == null)
                return null;

            ITrackingSpan applicableToSpan = point.Value.Snapshot.CreateTrackingSpan(item.Span, SpanTrackingMode.EdgeNegative);

            // Switch to UI thread before creating WPF controls
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (item.Errors.Any())
            {
                foreach (DisplayError error in item.Errors)
                {
                    var element = new ContainerElement(ContainerElementStyle.Wrapped, new Shared.EditorTooltip(error));
                    return new QuickInfoItem(applicableToSpan, element);
                }
            }

            Property property = _document.PropertyAtPosition(point.Value);

            SchemaCatalog.TryGetKeyword(property?.Keyword?.Text, out Keyword keyword);

            // Keyword
            if (keyword != null && item.ItemType == ItemType.Keyword)
            {
                var element = new ContainerElement(ContainerElementStyle.Wrapped, new Shared.EditorTooltip(keyword));
                return new QuickInfoItem(applicableToSpan, element);
            }

            // Value
            else if (keyword != null && item.ItemType == ItemType.Value)
            {
                Value value = keyword.Values.FirstOrDefault(v => v.Name.Is(item.Text));

                if (value != null && !string.IsNullOrEmpty(value.Description))
                {
                    var element = new ContainerElement(ContainerElementStyle.Wrapped, new Shared.EditorTooltip(value));
                    return new QuickInfoItem(applicableToSpan, element);
                }
            }

            // Severity
            else if (item.ItemType == ItemType.Severity && SchemaCatalog.TryGetSeverity(item.Text, out Severity severity))
            {
                var element = new ContainerElement(ContainerElementStyle.Wrapped, new Shared.EditorTooltip(severity));
                return new QuickInfoItem(applicableToSpan, element);
            }

            // Suppression
            else if (item.ItemType == ItemType.Suppression && ErrorCatalog.TryGetErrorCode(item.Text, out var code))
            {
                var element = new ContainerElement(ContainerElementStyle.Wrapped, new Shared.EditorTooltip(code));
                return new QuickInfoItem(applicableToSpan, element);
            }

            return null;
        }

        public void Dispose()
        {
        }
    }
}
