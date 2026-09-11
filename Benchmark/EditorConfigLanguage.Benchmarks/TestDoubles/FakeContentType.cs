using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfigLanguage.Benchmarks.TestDoubles
{
    /// <summary>Minimal content type used by <see cref="FakeTextBuffer"/>.</summary>
    internal sealed class FakeContentType : IContentType
    {
        public static readonly FakeContentType Instance = new();

        public string TypeName => "editorconfig";

        public string DisplayName => "EditorConfig";

        public IEnumerable<IContentType> BaseTypes => Array.Empty<IContentType>();

        public bool IsOfType(string type) => string.Equals(type, TypeName, StringComparison.OrdinalIgnoreCase);
    }
}
