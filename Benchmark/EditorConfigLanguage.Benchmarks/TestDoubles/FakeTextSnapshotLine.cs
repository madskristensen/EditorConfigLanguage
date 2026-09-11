using Microsoft.VisualStudio.Text;

namespace EditorConfigLanguage.Benchmarks.TestDoubles
{
    /// <summary>Text snapshot line used by <see cref="FakeTextSnapshot"/>.</summary>
    internal sealed class FakeTextSnapshotLine : ITextSnapshotLine
    {
        private readonly string _lineText;
        private readonly string _lineBreak;

        public FakeTextSnapshotLine(FakeTextSnapshot snapshot, int lineNumber, int start, string lineText, string lineBreak)
        {
            Snapshot = snapshot;
            LineNumber = lineNumber;
            Start = new SnapshotPoint(snapshot, start);
            _lineText = lineText;
            _lineBreak = lineBreak;
        }

        public ITextSnapshot Snapshot { get; }

        public int LineNumber { get; }

        public SnapshotPoint Start { get; }

        public int Length => _lineText.Length;

        public int LengthIncludingLineBreak => _lineText.Length + _lineBreak.Length;

        public int LineBreakLength => _lineBreak.Length;

        public SnapshotPoint End => new(Snapshot, Start.Position + Length);

        public SnapshotPoint EndIncludingLineBreak => new(Snapshot, Start.Position + LengthIncludingLineBreak);

        public SnapshotSpan Extent => new(Start, End);

        public SnapshotSpan ExtentIncludingLineBreak => new(Start, EndIncludingLineBreak);

        public string GetText() => _lineText;

        public string GetTextIncludingLineBreak() => _lineText + _lineBreak;

        public string GetLineBreakText() => _lineBreak;
    }
}
