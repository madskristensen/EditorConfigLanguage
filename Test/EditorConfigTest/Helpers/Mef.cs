using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;

namespace EditorConfigTest
{
    public class Mef
    {
        [Import]
        private ITextBufferFactoryService TextBufferService { get; set; }

        [Import]
        private IContentTypeRegistryService ContentTypeService { get; set; }

        public Mef()
        {
            var lib = new DirectoryInfo(@"..\..\..\..\lib\").FullName;

            var editor = Assembly.LoadFrom(lib + "Microsoft.VisualStudio.Platform.VSEditor.dll");
            var interop = Assembly.LoadFrom(lib + "Microsoft.VisualStudio.Platform.VSEditor.interop.dll");
            var text = Assembly.LoadFrom(lib + "Microsoft.VisualStudio.Text.Internal.dll");

            AggregateCatalog catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(editor));
            catalog.Catalogs.Add(new AssemblyCatalog(interop));
            catalog.Catalogs.Add(new AssemblyCatalog(text));

            catalog.Catalogs.Add(new AssemblyCatalog(typeof(DocumentTest).Assembly));
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(EditorConfig.EditorConfigDocument).Assembly));

            CompositionContainer container = new CompositionContainer(catalog);
            container.SatisfyImportsOnce(this);
        }

        public static ITextBuffer CreateTextBuffer(string text)
        {
            var mef =  new Mef();
            return mef.TextBufferService.CreateTextBuffer(text, mef.ContentTypeService.UnknownContentType);
        }
    }
}
