using EditorConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    public class CompletionTest
    {
        [TestMethod]
        public void GetHighlightedSpans()
        {
            var spans = FilteredCompletionSet.GetHighlightedSpans("dotnet_style_predefined_type_for_locals_parameters_members", "dottymem");
            Assert.AreEqual(3, spans.Count);

            var ttw = FilteredCompletionSet.GetHighlightedSpans("trim_trailing_whitespace", "trimtrailingwhitespace");
            Assert.AreEqual(3, ttw.Count);
            Assert.AreEqual(4, ttw[0].Length);

            var partlyNonMatch = FilteredCompletionSet.GetHighlightedSpans("dotnet_style_explicit_tuple_names", "dottymem");
            Assert.AreEqual(0, partlyNonMatch.Count);

            var partlyNonMatch2 = FilteredCompletionSet.GetHighlightedSpans("dotnet_style_qualification_for_event", "dotmem");
            Assert.AreEqual(0, partlyNonMatch2.Count);

            var partlyNonMatch3 = FilteredCompletionSet.GetHighlightedSpans("dotnet_style_qualification_for_method", "dotmem");
            Assert.AreEqual(0, partlyNonMatch3.Count);

            var nonMatch = FilteredCompletionSet.GetHighlightedSpans("test", "l");
            Assert.AreEqual(0, nonMatch.Count);

            var cha = FilteredCompletionSet.GetHighlightedSpans("charset", "CHAR");
            Assert.AreEqual(1, cha.Count);
        }
    }
}
