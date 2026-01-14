using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    public class ErrorCatalogTest
    {
        [TestMethod]
        public void TryGetErrorCode_ValidCode_ReturnsTrue()
        {
            bool result = ErrorCatalog.TryGetErrorCode("EC101", out Error error);

            Assert.IsTrue(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("EC101", error.Code);
        }

        [TestMethod]
        public void TryGetErrorCode_InvalidCode_ReturnsFalse()
        {
            bool result = ErrorCatalog.TryGetErrorCode("EC999", out Error error);

            Assert.IsFalse(result);
            Assert.IsNull(error);
        }

        [TestMethod]
        public void TryGetErrorCode_NullCode_ReturnsFalse()
        {
            bool result = ErrorCatalog.TryGetErrorCode(null, out Error error);

            Assert.IsFalse(result);
            Assert.IsNull(error);
        }

        [TestMethod]
        public void All_ContainsExpectedErrorCodes()
        {
            string[] expectedCodes = ["EC101", "EC102", "EC103", "EC104", "EC105", "EC106", "EC107", "EC108", "EC109", "EC110"];

            foreach (string code in expectedCodes)
            {
                bool exists = ErrorCatalog.TryGetErrorCode(code, out _);
                Assert.IsTrue(exists, $"Error code '{code}' should exist in catalog");
            }
        }

        [TestMethod]
        public void DuplicateProperty_HasCorrectCode()
        {
            Assert.AreEqual("EC101", ErrorCatalog.DuplicateProperty.Code);
            Assert.AreEqual(ErrorCategory.Warning, ErrorCatalog.DuplicateProperty.Category);
        }

        [TestMethod]
        public void DuplicateSection_HasCorrectCode()
        {
            Assert.AreEqual("EC102", ErrorCatalog.DuplicateSection.Code);
            Assert.AreEqual(ErrorCategory.Warning, ErrorCatalog.DuplicateSection.Category);
        }

        [TestMethod]
        public void MissingSeverity_HasCorrectCategory()
        {
            Assert.AreEqual("EC104", ErrorCatalog.MissingSeverity.Code);
            Assert.AreEqual(ErrorCategory.Error, ErrorCatalog.MissingSeverity.Category);
        }

        [TestMethod]
        public void GlobbingNoMatch_IsSuggestion()
        {
            Assert.AreEqual("EC103", ErrorCatalog.GlobbingNoMatch.Code);
            Assert.AreEqual(ErrorCategory.Suggestion, ErrorCatalog.GlobbingNoMatch.Category);
        }

        [TestMethod]
        public void SectionSyntaxError_IsError()
        {
            Assert.AreEqual("EC109", ErrorCatalog.SectionSyntaxError.Code);
            Assert.AreEqual(ErrorCategory.Error, ErrorCatalog.SectionSyntaxError.Category);
        }

        [TestMethod]
        public void UnknownKeyword_IsWarning()
        {
            Assert.AreEqual("EC112", ErrorCatalog.UnknownKeyword.Code);
            Assert.AreEqual(ErrorCategory.Warning, ErrorCatalog.UnknownKeyword.Category);
        }

        [TestMethod]
        public void All_HasNoDuplicateCodes()
        {
            var codes = new System.Collections.Generic.HashSet<string>();

            foreach (Error error in ErrorCatalog.All)
            {
                Assert.DoesNotContain(error.Code, codes, $"Duplicate error code found: {error.Code}");
                codes.Add(error.Code);
            }
        }

        [TestMethod]
        public void All_AllErrorsHaveMessages()
        {
            foreach (Error error in ErrorCatalog.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(error.Description), $"Error {error.Code} should have a description");
            }
        }
    }
}
