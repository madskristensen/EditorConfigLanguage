using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    internal class SectionSignatureHelpSource : ISignatureHelpSource
    {
        private ITextBuffer _buffer;
        private ITrackingSpan _span;

        public SectionSignatureHelpSource(ITextBuffer textBuffer)
        {
            _buffer = textBuffer;
        }

        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
        {
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (!point.HasValue)
                return;

            var line = point.Value.GetContainingLine();
            var lineText = line.GetText().Trim();

            if (!lineText.StartsWith("[", StringComparison.Ordinal))
                return;

            _span = _buffer.CurrentSnapshot.CreateTrackingSpan(line.Extent, SpanTrackingMode.EdgeNegative);

            signatures.Add(new SectionSignature("[*.cs]", "Matches files with a specific file extension", _span, session));
            signatures.Add(new SectionSignature("[*.{cs,vb}]", "Matches multiple files with brace expansion notation", _span, session));
            signatures.Add(new SectionSignature("[app/**.js]", "Matches all JS under lib directory", _span, session));
            signatures.Add(new SectionSignature("[{package.json,.npmrc}]", "Matches the exact files - either package.json or .npmrc", _span, session));
        }

        public ISignature GetBestMatch(ISignatureHelpSession session)
        {
            if (session.Signatures.Count != 4)
                return session.Signatures.FirstOrDefault();

            var text = _span.GetText(_buffer.CurrentSnapshot);

            if (text.Contains("[{"))
                return session.Signatures.ElementAt(3);

            if (text.Contains("{"))
                return session.Signatures.ElementAt(1);

            if (text.Contains("**"))
                return session.Signatures.ElementAt(2);

            return session.Signatures.ElementAt(0);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}