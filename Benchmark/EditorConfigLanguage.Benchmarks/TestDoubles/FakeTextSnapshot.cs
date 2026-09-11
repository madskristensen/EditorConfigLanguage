using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfigLanguage.Benchmarks.TestDoubles
{
    /// <summary>Minimal immutable text snapshot over an in-memory string.</summary>
    internal sealed class FakeTextSnapshot : ITextSnapshot
    {
        private readonly string _text;
        private readonly List<FakeTextSnapshotLine> _lines;

        public FakeTextSnapshot(ITextBuffer buffer, string text)
        {
            TextBuffer = buffer;
            _text = text ?? string.Empty;
            _lines = BuildLines(this, _text);
        }

        public ITextBuffer TextBuffer { get; }

        public IContentType ContentType => TextBuffer.ContentType;

        public ITextVersion Version => null;

        public int Length => _text.Length;

        public int LineCount => _lines.Count;

        public char this[int position] => _text[position];

        public IEnumerable<ITextSnapshotLine> Lines => _lines;

        public string GetText() => _text;

        public string GetText(Span span) => _text.Substring(span.Start, span.Length);

        public string GetText(int startIndex, int length) => _text.Substring(startIndex, length);

        public char[] ToCharArray(int startIndex, int length) => _text.Substring(startIndex, length).ToCharArray();

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            => _text.CopyTo(sourceIndex, destination, destinationIndex, count);

        public ITextSnapshotLine GetLineFromLineNumber(int lineNumber) => _lines[lineNumber];

        public ITextSnapshotLine GetLineFromPosition(int position)
        {
            foreach (FakeTextSnapshotLine line in _lines)
            {
                if (position >= line.Start.Position && position <= line.Start.Position + line.LengthIncludingLineBreak)
                    return line;
            }

            return _lines[_lines.Count - 1];
        }

        public int GetLineNumberFromPosition(int position) => GetLineFromPosition(position).LineNumber;

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
            => throw new NotSupportedException("Not exercised by the benchmarked EditorConfigDocument code paths.");

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
            => throw new NotSupportedException("Not exercised by the benchmarked EditorConfigDocument code paths.");

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
            => throw new NotSupportedException("Not exercised by the benchmarked EditorConfigDocument code paths.");

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
            => throw new NotSupportedException("Not exercised by the benchmarked EditorConfigDocument code paths.");

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
            => throw new NotSupportedException("Not exercised by the benchmarked EditorConfigDocument code paths.");

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
            => throw new NotSupportedException("Not exercised by the benchmarked EditorConfigDocument code paths.");

        public void Write(TextWriter writer) => writer.Write(_text);

        public void Write(TextWriter writer, Span span) => writer.Write(GetText(span));

        private static List<FakeTextSnapshotLine> BuildLines(FakeTextSnapshot snapshot, string text)
        {
            var lines = new List<FakeTextSnapshotLine>();
            int position = 0;
            int lineNumber = 0;

            while (position <= text.Length)
            {
                int newlineIndex = text.IndexOf('\n', position);

                if (newlineIndex < 0)
                {
                    lines.Add(new FakeTextSnapshotLine(snapshot, lineNumber, position, text.Substring(position), string.Empty));
                    break;
                }

                int lineEnd = newlineIndex;
                string lineBreak = "\n";

                if (lineEnd > position && text[lineEnd - 1] == '\r')
                {
                    lineEnd--;
                    lineBreak = "\r\n";
                }

                lines.Add(new FakeTextSnapshotLine(snapshot, lineNumber, position, text.Substring(position, lineEnd - position), lineBreak));

                position = newlineIndex + 1;
                lineNumber++;
            }

            return lines;
        }
    }
}
