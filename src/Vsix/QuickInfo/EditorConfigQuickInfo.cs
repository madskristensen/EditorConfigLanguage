using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

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

                if (_control == null)
                {
                    var image = new Image();
                    image.Source = GetImage(KnownMonikers.VisualStudioSettingsFile, 16);
                    _control = new QuickInfoControl(image);
                }

                _control.Keyword.Text = keyword;
                _control.Description.Text = item.IsSupported ? item.Description : $"Not supported by Visual Studio\r\n\r\n{item.Description}"; ;
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

        public static BitmapSource GetImage(ImageMoniker moniker, int size)
        {
            ImageAttributes imageAttributes = new ImageAttributes
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                Format = (uint)_UIDataFormat.DF_WPF,
                LogicalHeight = size,
                LogicalWidth = size,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes))
            };

            var service = (IVsImageService2)Package.GetGlobalService(typeof(SVsImageService));
            IVsUIObject result = service.GetImage(moniker, imageAttributes);
            result.get_Data(out object data);

            if (data == null)
                return null;

            return data as BitmapSource;
        }
    }
}
