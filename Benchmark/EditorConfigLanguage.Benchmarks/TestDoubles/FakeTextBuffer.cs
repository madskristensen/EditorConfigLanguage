using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfigLanguage.Benchmarks.TestDoubles
{
    /// <summary>Minimal text buffer that drives the parser without a Visual Studio host.</summary>
    internal sealed class FakeTextBuffer : ITextBuffer
    {
        public FakeTextBuffer(string initialText, IContentType contentType = null)
        {
            ContentType = contentType ?? FakeContentType.Instance;
            Properties = new PropertyCollection();
            CurrentSnapshot = new FakeTextSnapshot(this, initialText ?? string.Empty);
        }

        public IContentType ContentType { get; private set; }

        public ITextSnapshot CurrentSnapshot { get; private set; }

        public PropertyCollection Properties { get; }

        public bool EditInProgress => false;

        public event EventHandler<TextContentChangedEventArgs> Changed;

        public event EventHandler<TextContentChangedEventArgs> ChangedLowPriority { add { } remove { } }

        public event EventHandler<TextContentChangedEventArgs> ChangedHighPriority { add { } remove { } }

        public event EventHandler<TextContentChangingEventArgs> Changing { add { } remove { } }

        public event EventHandler PostChanged { add { } remove { } }

        public event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged { add { } remove { } }

        public event EventHandler<SnapshotSpanEventArgs> ReadOnlyRegionsChanged { add { } remove { } }

        public void SimulateEdit(string newText)
        {
            ITextSnapshot before = CurrentSnapshot;
            CurrentSnapshot = new FakeTextSnapshot(this, newText ?? string.Empty);
            Changed?.Invoke(this, new TextContentChangedEventArgs(before, CurrentSnapshot, default, null));
        }

        public ITextEdit CreateEdit() => throw NotSupported();

        public ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag) => throw NotSupported();

        public IReadOnlyRegionEdit CreateReadOnlyRegionEdit() => throw NotSupported();

        public void TakeThreadOwnership() { }

        public bool CheckEditAccess() => true;

        public void ChangeContentType(IContentType newContentType, object editTag) => throw NotSupported();

        public ITextSnapshot Insert(int position, string text) => throw NotSupported();

        public ITextSnapshot Delete(Span deleteSpan) => throw NotSupported();

        public ITextSnapshot Replace(Span replaceSpan, string replaceWith) => throw NotSupported();

        public bool IsReadOnly(int position) => false;

        public bool IsReadOnly(int position, bool isEdit) => false;

        public bool IsReadOnly(Span span) => false;

        public bool IsReadOnly(Span span, bool isEdit) => false;

        public NormalizedSpanCollection GetReadOnlyExtents(Span span) => new();

        private static NotSupportedException NotSupported([System.Runtime.CompilerServices.CallerMemberName] string member = null)
            => new($"FakeTextBuffer.{member} is not exercised by the benchmarked EditorConfigDocument code paths.");
    }
}
