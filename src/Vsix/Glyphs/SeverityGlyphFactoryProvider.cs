using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using System;

namespace EditorConfig
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("Severity Glyph")]
    [Order(Before = "VsTextMarker")]
    [ContentType(Constants.LanguageName)]
    [TagType(typeof(SeverityTag))]
    public class SeverityGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return view.Properties.GetOrCreateSingletonProperty(() => new SeverityGlyphFactory());
        }
    }
}
