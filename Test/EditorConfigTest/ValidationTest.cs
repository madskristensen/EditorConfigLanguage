using System.Linq;
using System.Threading.Tasks;
using EditorConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;

namespace EditorConfigTest
{
    [TestClass]
    public class ValidationTest
    {
        //[TestMethod, TestCategory("MEF")]
        //public async Task ValidateSeverity()
        //{
        //    EditorConfigPackage.ValidationOptions = new ValidationOptions();
        //    ITextBuffer buffer = Mef.CreateTextBuffer(Samples.SeveritySimple);
        //    var doc = EditorConfigDocument.FromTextBuffer(buffer);
        //    var validator = EditorConfigValidator.FromDocument(doc);

        //    await validator.RequestValidationAsync(true);

        //    await Task.Delay(1000);

        //    Assert.IsTrue(doc.ParseItems[2].HasErrors);
        //}
    }
}
