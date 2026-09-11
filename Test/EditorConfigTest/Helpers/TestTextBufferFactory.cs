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

            var lines = new List<ITextSnapshotLine>();
            int start = 0;

            while (start <= text.Length)
            {
                int end = text.IndexOfAny(['\r', '\n'], start);
                end = end < 0 ? text.Length : end;
                string lineText = text.Substring(start, end - start);
                int lineStart = start;

                var line = new Mock<ITextSnapshotLine>();
                line.Setup(value => value.GetText()).Returns(lineText);
                line.SetupGet(value => value.Snapshot).Returns(snapshot.Object);
                line.SetupGet(value => value.Start).Returns(() => new SnapshotPoint(snapshot.Object, lineStart));
                lines.Add(line.Object);

                if (end == text.Length)
                    break;

                start = end + (text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n' ? 2 : 1);
            }

            snapshot.SetupGet(value => value.Lines).Returns(lines);

            var buffer = new Mock<ITextBuffer>();
            buffer.SetupGet(value => value.CurrentSnapshot).Returns(snapshot.Object);
            buffer.SetupGet(value => value.Properties).Returns(new PropertyCollection());
            return buffer.Object;
        }
    }
}
