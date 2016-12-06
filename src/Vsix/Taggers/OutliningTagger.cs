using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace EditorConfig
{
    internal sealed class OutliningTagger : ITagger<IOutliningRegionTag>, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private ITextSnapshot _snapshot;
        private bool _hasBufferchanged;
        private Timer _timer;
        private bool _isParsing;

        public OutliningTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _snapshot = buffer.CurrentSnapshot;
            Regions = new List<Region>();
            ReParse();
            _buffer.Changed += BufferChanged;

            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                _timer = new Timer(1000);
                _timer.Elapsed += Timer_Elapsed;
                _timer.Start();
            });
        }

        public IEnumerable<Region> Regions { get; private set; }

        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_hasBufferchanged || _isParsing)
                return;

            _isParsing = true;
            _timer.Stop();

            ReParse();

            TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(_snapshot, 0, _snapshot.Length)));

            _timer.Start();
            _hasBufferchanged = false;
            _isParsing = false;
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            IEnumerable<Region> currentRegions = Regions;
            ITextSnapshot currentSnapshot = _snapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int startLineNumber = entire.Start.GetContainingLine().LineNumber;
            int endLineNumber = entire.End.GetContainingLine().LineNumber;

            foreach (var region in currentRegions)
            {
                if (region.StartLine <= endLineNumber && region.EndLine >= startLineNumber)
                {
                    var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                    string text = startLine.GetText();
                    string hover = entire.Snapshot.GetText(region.StartOffset, region.EndOffset - region.StartOffset);

                    yield return new TagSpan<IOutliningRegionTag>(
                        new SnapshotSpan(currentSnapshot, region.StartOffset, region.EndOffset - region.StartOffset),
                        new OutliningRegionTag(false, false, text, hover));
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (e.After != _buffer.CurrentSnapshot || _isParsing)
                return;

            _hasBufferchanged = true;
        }

        void ReParse()
        {
            ITextSnapshot newSnapshot = _buffer.CurrentSnapshot;
            List<Region> newRegions = new List<Region>();
            Region currentRegion = null;
            ITextSnapshotLine prev = null;

            foreach (var line in newSnapshot.Lines)
            {
                string text = line.GetText();

                if (!string.IsNullOrWhiteSpace(text) && text[0] == '[' && currentRegion == null)
                {
                    currentRegion = new Region
                    {
                        StartLine = line.LineNumber,
                        StartOffset = line.Start.Position
                    };
                }
                else if (currentRegion != null)
                {
                    if (line.LineNumber == newSnapshot.LineCount - 1 && !string.IsNullOrWhiteSpace(text))
                    {
                        currentRegion.EndLine = line.LineNumber;
                        currentRegion.EndOffset = line.End.Position;
                        newRegions.Add(currentRegion);
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(text) || text[0] == '[')
                    {
                        currentRegion.EndLine = prev.LineNumber;
                        currentRegion.EndOffset = prev.End.Position;
                        newRegions.Add(currentRegion);

                        currentRegion = null;
                    }

                    if (!string.IsNullOrWhiteSpace(text) && text[0] == '[')
                    {
                        currentRegion = new Region
                        {
                            StartLine = line.LineNumber,
                            StartOffset = line.Start.Position
                        };
                    }
                }

                prev = line;
            }

            _snapshot = newSnapshot;
            Regions = newRegions.Where(line => line.StartLine != line.EndLine);
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }

    class Region
    {
        public int StartLine { get; set; }
        public int StartOffset { get; set; }
        public int EndLine { get; set; }
        public int EndOffset { get; set; }
    }
}
