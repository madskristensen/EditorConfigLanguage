using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

using Moq;

namespace EditorConfigTest
{
    public static class TestTextBufferFactory
    {
        public static ITextBuffer CreateTextBuffer(string text)
        {
            var snapshot = new Mock<ITextSnapshot>();
            snapshot.SetupGet(value => value.Length).Returns(text.Length);
            snapshot.Setup(value => value.GetText()).Returns(text);
            snapshot.Setup(value => value.GetText(It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int position, int length) => text.Substring(position, length));
            snapshot.Setup(value => value.GetText(It.IsAny<Span>()))
                .Returns((Span span) => text.Substring(span.Start, span.Length));

            var lines = new List<ITextSnapshotLine>();
            var lineStarts = new List<int>();
            int start = 0;

            while (start <= text.Length)
            {
                int end = text.IndexOfAny(['\r', '\n'], start);
                end = end < 0 ? text.Length : end;
                string lineText = text.Substring(start, end - start);
                int lineStart = start;
                int breakLength = end == text.Length ? 0 : text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n' ? 2 : 1;

                var line = new Mock<ITextSnapshotLine>();
                line.Setup(value => value.GetText()).Returns(lineText);
                line.SetupGet(value => value.Snapshot).Returns(snapshot.Object);
                line.SetupGet(value => value.Start).Returns(() => new SnapshotPoint(snapshot.Object, lineStart));
                line.SetupGet(value => value.End).Returns(() => new SnapshotPoint(snapshot.Object, lineStart + lineText.Length));
                line.SetupGet(value => value.EndIncludingLineBreak).Returns(() => new SnapshotPoint(snapshot.Object, lineStart + lineText.Length + breakLength));
                line.SetupGet(value => value.Length).Returns(lineText.Length);
                line.SetupGet(value => value.Extent).Returns(() => new SnapshotSpan(snapshot.Object, lineStart, lineText.Length));
                line.SetupGet(value => value.ExtentIncludingLineBreak).Returns(() => new SnapshotSpan(snapshot.Object, lineStart, lineText.Length + breakLength));
                line.SetupGet(value => value.LineNumber).Returns(lines.Count);
                lineStarts.Add(lineStart);
                lines.Add(line.Object);

                if (end == text.Length)
                    break;

                start = end + (text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n' ? 2 : 1);
            }

            snapshot.SetupGet(value => value.Lines).Returns(lines);
            snapshot.SetupGet(value => value.LineCount).Returns(lines.Count);
            snapshot.Setup(value => value.GetLineFromLineNumber(It.IsAny<int>())).Returns((int number) => lines[number]);
            snapshot.Setup(value => value.GetLineFromPosition(It.IsAny<int>()))
                .Returns((int position) => lines[lineStarts.FindLastIndex(lineStart => lineStart <= position)]);

            var buffer = new Mock<ITextBuffer>();
            buffer.SetupGet(value => value.CurrentSnapshot).Returns(snapshot.Object);
            buffer.SetupGet(value => value.Properties).Returns(new PropertyCollection());
            snapshot.SetupGet(value => value.TextBuffer).Returns(buffer.Object);
            return buffer.Object;
        }

        internal static ITextSnapshot SetText(ITextBuffer buffer, string text)
        {
            ITextSnapshot before = buffer.CurrentSnapshot;
            ITextSnapshot after = CreateTextBuffer(text).CurrentSnapshot;
            Mock.Get(after).SetupGet(snapshot => snapshot.TextBuffer).Returns(buffer);
            Mock<ITextBuffer> mock = Mock.Get(buffer);
            mock.SetupGet(value => value.CurrentSnapshot).Returns(after);
            mock.Raise(value => value.Changed += null, new TextContentChangedEventArgs(before, after, EditOptions.None, null));
            return after;
        }
    }
}
