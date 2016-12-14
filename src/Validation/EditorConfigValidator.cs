using System;
using System.Linq;
using System.Timers;

namespace EditorConfig
{
    class EditorConfigValidator : IDisposable
    {
        private EditorConfigDocument _document;
        private const int _validationDelay = 1000;
        private Timer _timer;
        private bool _hasChanged;
        private bool _prevEnabled = EditorConfigPackage.ValidationOptions.EnableValidation;

        private EditorConfigValidator(EditorConfigDocument document)
        {
            _document = document;
            _document.Parsed += DocumentParsed;

            if (_prevEnabled)
                Validate();

            ValidationOptions.Saved += DocumentParsed;
        }

        public static EditorConfigValidator FromDocument(EditorConfigDocument document)
        {
            return document.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigValidator(document));
        }

        private void DocumentParsed(object sender, EventArgs e)
        {
            if (!EditorConfigPackage.ValidationOptions.EnableValidation)
            {
                // Don't run the logic unless the user changed the settings since last run
                if (_prevEnabled != EditorConfigPackage.ValidationOptions.EnableValidation)
                {
                    foreach (var item in _document.ParseItems.Where(i => i.Errors.Any()))
                    {
                        item.Errors.Clear();
                    }

                    Validated?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _hasChanged = true;

                if (_timer == null)
                {
                    _timer = new Timer(1000);
                    _timer.Elapsed += TimerElapsed;
                }

                _timer.Enabled = true;
            }

            _prevEnabled = EditorConfigPackage.ValidationOptions.EnableValidation;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();

            if (_hasChanged && !_document.IsParsing)
                Validate();

            _hasChanged = false;
        }

        private void Validate()
        {
            try
            {
                foreach (var item in _document.ParseItems.Where(i => i.ItemType == ItemType.Unknown))
                {
                    ValidateUnknown(item);
                }

                ValidateSection();

                foreach (var property in _document.Properties)
                {
                    ValidateProperty(property);
                }

                Validated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }
        }

        private void ValidateUnknown(ParseItem item)
        {
            item.AddError("Syntax error. Element not valid at current location");
        }

        private void ValidateSection()
        {
            foreach (var section in _document.Sections)
            {
                foreach (var property in section.Properties)
                {
                    ValidateProperty(property);

                    // Duplicate property
                    if (EditorConfigPackage.ValidationOptions.EnableDuplicateProperties)
                    {
                        if (section.Properties.First(p => p.Keyword.Text.Equals(property.Keyword.Text, StringComparison.OrdinalIgnoreCase)) != property)
                            property.Keyword.AddError(Resources.Text.ValidationDuplicateProperty);
                    }

                    // Root in section
                    if (property.Keyword.Text.Equals(SchemaCatalog.Root, StringComparison.OrdinalIgnoreCase))
                    {
                        property.Keyword.AddError(Resources.Text.ValidationRootInSection);
                    }
                }

                // Duplicate section
                if (EditorConfigPackage.ValidationOptions.EnableDuplicateSections)
                {
                    if (_document.Sections.First(s => s.Item.Text == section.Item.Text) != section)
                        section.Item.AddError(string.Format(Resources.Text.ValidationDuplicateSection, section.Item.Text));
                }
            }
        }

        private void ValidateProperties()
        {
            foreach (var property in _document.Properties)
            {
                // Only root property allowed
                if (property != _document.Root)
                    property.Keyword.AddError(Resources.Text.ValidationRootInSection);
            }
        }

        private void ValidateProperty(Property property)
        {
            // Keyword
            if (EditorConfigPackage.ValidationOptions.EnableUnknownProperties & !SchemaCatalog.TryGetProperty(property.Keyword.Text, out Keyword keyword))
            {
                property.Keyword.AddError(string.Format(Resources.Text.ValidateUnknownKeyword, property.Keyword.Text));
            }

            // Missing value
            else if (property.Value == null)
            {
                property.Keyword.AddError("A value must be specified");
            }
            // Value not in schema
            else if (EditorConfigPackage.ValidationOptions.EnableUnknownValues &&
                !keyword.Values.Any(v => v.Name.Equals(property.Value?.Text, StringComparison.OrdinalIgnoreCase)) &&
                !(int.TryParse(property.Value.Text, out int intValue) && intValue > 0))
            {
                property.Value.AddError(string.Format(Resources.Text.InvalidValue, property.Value.Text, keyword.Name));
            }

            // Missing severity
            else if (property.Severity == null && property.Value.Text.Equals("true", StringComparison.OrdinalIgnoreCase) && keyword.SupportsSeverity)
            {
                property.Value.AddError(Resources.Text.ValidationMissingSeverity);
            }
            else if (property.Severity != null)
            {
                // Severity not applicaple to property
                if (!keyword.SupportsSeverity)
                {
                    property.Severity.AddError(string.Format("The \"{0}\" property does not support a severity suffix", keyword.Name));
                }
                // Severity not in schema
                else if (!SchemaCatalog.TryGetSeverity(property.Severity.Text, out Severity severity))
                {
                    property.Severity.AddError(string.Format(Resources.Text.ValidationInvalidSeverity, property.Severity.Text));
                }
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            Validated = null;

            _document.Parsed -= DocumentParsed;
            ValidationOptions.Saved -= DocumentParsed;
        }

        public event EventHandler Validated;
    }
}
