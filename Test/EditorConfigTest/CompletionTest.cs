using System.Collections.Generic;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;

namespace EditorConfigTest
{
    [TestClass]
    public class CompletionTest
    {
        [TestMethod]
        public void GetHighlightedSpans()
        {
            List<Span> spans = FilteredCompletionSet.GetHighlightedSpans("dotnet_style_predefined_type_for_locals_parameters_members", "dottymem");
            Assert.HasCount(3, spans);

            List<Span> ttw = FilteredCompletionSet.GetHighlightedSpans("trim_trailing_whitespace", "trimtrailingwhitespace");
            Assert.HasCount(3, ttw);
            Assert.AreEqual(4, ttw[0].Length);

            List<Span> partlyNonMatch = FilteredCompletionSet.GetHighlightedSpans("dotnet_style_explicit_tuple_names", "dottymem");
            Assert.IsEmpty(partlyNonMatch);

            List<Span> partlyNonMatch2 = FilteredCompletionSet.GetHighlightedSpans("dotnet_style_qualification_for_event", "dotmem");
            Assert.IsEmpty(partlyNonMatch2);

            List<Span> partlyNonMatch3 = FilteredCompletionSet.GetHighlightedSpans("dotnet_style_qualification_for_method", "dotmem");
            Assert.IsEmpty(partlyNonMatch3);

            List<Span> nonMatch = FilteredCompletionSet.GetHighlightedSpans("test", "l");
            Assert.IsEmpty(nonMatch);

            List<Span> cha = FilteredCompletionSet.GetHighlightedSpans("charset", "CHAR");
            Assert.HasCount(1, cha);
        }
    }
}
