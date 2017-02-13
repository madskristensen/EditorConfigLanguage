using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    class SuppressErrorAction : BaseSuggestedAction
    {
        private const string _suppressFormat = "# Suppress: {0}";
        private EditorConfigDocument _document;
        string _errorCode;

        public SuppressErrorAction(EditorConfigDocument document, string errorCode)
        {
            _document = document;
            _errorCode = errorCode;
        }

        public override string DisplayText
        {
            get { return $"Suppress {_errorCode}"; }
        }

        public override ImageMoniker IconMoniker => KnownMonikers.ValidationRule;

        public override bool IsEnabled
        {
            get
            {
                return !_document.Suppressions.Contains(_errorCode, StringComparer.OrdinalIgnoreCase);
            }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            var validator = EditorConfigValidator.FromDocument(_document);
            validator.SuppressError(_errorCode);
        }
    }
}
