using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System.Linq;
using System.Windows.Threading;

namespace EditorConfig
{
    internal class SectionSignatureHelpSource : ISignatureHelpSource
    {
        private ITextBuffer _buffer;
        private EditorConfigDocument _document;

        public SectionSignatureHelpSource(ITextBuffer textBuffer)
        {
            _buffer = textBuffer;
            _document = EditorConfigDocument.FromTextBuffer(textBuffer);
        }

        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
        {
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (!point.HasValue)
                return;

            var line = _buffer.CurrentSnapshot.GetLineFromPosition(point.Value);
            var lineText = line.GetText().Trim();

            if (!lineText.StartsWith("[", StringComparison.Ordinal))
                return;

            var span = _buffer.CurrentSnapshot.CreateTrackingSpan(line.Extent, SpanTrackingMode.EdgeNegative);

            signatures.Add(new SectionSignature("[*.cs]", "Matches files with a specific file extension", span, session));
            signatures.Add(new SectionSignature("[*.{cs,vb}]", "Matches multiple files with brace expansion notation", span, session));
            signatures.Add(new SectionSignature("[app/**.js]", "Matches all JS under lib directory", span, session));
            signatures.Add(new SectionSignature("[{package.json,.npmrc}]", "Matches the exact files - either package.json or .npmrc", span, session));
        }

        public ISignature GetBestMatch(ISignatureHelpSession session)
        {
            return session.Signatures.FirstOrDefault();
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}