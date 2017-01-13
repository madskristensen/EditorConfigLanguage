using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace EditorConfig
{

    internal class GenericParameter : IParameter
    {
        private SectionSignature _signature;

        public GenericParameter(SectionSignature signature)
        {
            _signature = signature;
        }

        public string Documentation
        {
            get { return null; }
        }

        public Span Locus
        {
            get { return new Span(0, _signature.PropertyName.Length); }
        }

        public string Name
        {
            get { return _signature.PropertyName; }
        }

        public Span PrettyPrintedLocus
        {
            get { return Locus; }
        }

        public ISignature Signature
        {
            get { return _signature; }
        }
    }

}
