using Microsoft.VisualStudio.Shell;
using Minimatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace EditorConfig
{
    class EditorConfigValidator : IDisposable
    {
        private EditorConfigDocument _document;
        private const int _validationDelay = 500;
        private DateTime _lastRequestForValidation;
        private Timer _timer;
        private bool _hasChanged, _validating;
        private bool _prevEnabled = EditorConfigPackage.ValidationOptions.EnableValidation;
        private Dictionary<string, bool> _globbingCache = new Dictionary<string, bool>();

        private EditorConfigValidator(EditorConfigDocument document)
        {
            _document = document;
            _document.Parsed += DocumentParsedAsync;

            if (_prevEnabled)
                ValidateAsync().ConfigureAwait(false);

            ValidationOptions.Saved += DocumentParsedAsync;
        }

        public static EditorConfigValidator FromDocument(EditorConfigDocument document)
        {
            return document.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigValidator(document));
        }

        private async void DocumentParsedAsync(object sender, EventArgs e)
        {
            if (!EditorConfigPackage.ValidationOptions.EnableValidation)
            {
                // Don't run the logic unless the user changed the settings since last run
                if (_prevEnabled != EditorConfigPackage.ValidationOptions.EnableValidation)
                {
                    ClearAllErrors();
                    Validated?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                await RequestValidationAsync(false);
            }

            _prevEnabled = EditorConfigPackage.ValidationOptions.EnableValidation;
        }

        public async System.Threading.Tasks.Task RequestValidationAsync(bool force)
        {
            _lastRequestForValidation = DateTime.Now;

            if (force)
            {
                _globbingCache.Clear();
                ClearAllErrors();
                await ValidateAsync();
            }
            else
            {
                if (_timer == null)
                {
                    _timer = new Timer(_validationDelay);
                    _timer.Elapsed += TimerElapsedAsync;
                }

                _hasChanged = true;
                _timer.Enabled = true;
            }
        }

        private async void TimerElapsedAsync(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.AddMilliseconds(-_validationDelay) > _lastRequestForValidation && _hasChanged && !_document.IsParsing)
            {
                _timer.Stop();
                await ValidateAsync();
            }
        }

        private void ClearAllErrors()
        {
            foreach (var item in _document.ParseItems.Where(i => i.Errors.Any()))
            {
                item.Errors.Clear();
            }
        }

        private async System.Threading.Tasks.Task ValidateAsync()
        {
            if (_validating) return;

            _validating = true;

            await System.Threading.Tasks.Task.Run(() =>
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
                    Telemetry.TrackException("Validate", ex);
                }
                finally
                {
                    _hasChanged = false;
                    _validating = false;
                }
            });
        }

        private void ValidateUnknown(ParseItem item)
        {
            item.AddError(101, Resources.Text.ValidationUnknownElement, ErrorType.Error);
        }

        private void ValidateSection()
        {
            var parents = new List<EditorConfigDocument>();
            var parent = _document.Parent;

            while (parent != null)
            {
                parents.Add(parent);
                parent = parent.Parent;
            }

            foreach (var section in _document.Sections)
            {

                var parentSections = parents.SelectMany(d => d.Sections).Where(s => s.Item.Text == section.Item.Text);

                foreach (var property in section.Properties)
                {
                    ValidateProperty(property);

                    // Root in section
                    if (property.Keyword.Text.Equals(SchemaCatalog.Root, StringComparison.OrdinalIgnoreCase))
                    {
                        property.Keyword.AddError(102, Resources.Text.ValidationRootInSection, ErrorType.Error);
                    }

                    // Duplicate property
                    if (EditorConfigPackage.ValidationOptions.EnableDuplicateProperties && property.IsValid)
                    {
                        if (section.Properties.Last(p => p.Keyword.Text.Equals(property.Keyword.Text, StringComparison.OrdinalIgnoreCase)) != property)
                        {
                            property.Keyword.AddError(103, Resources.Text.ValidationDuplicateProperty, ErrorType.Suggestion);
                        }
                    }

                    // Parent duplicate
                    if (EditorConfigPackage.ValidationOptions.EnableDuplicateFoundInParent && property.IsValid && !property.Keyword.Errors.Any() && parentSections.Any())
                    {
                        var parentProperties = parentSections.SelectMany(s => s.Properties.Where(p => p.ToString() == property.ToString()));
                        if (parentProperties.Any())
                        {
                            var fileName = PackageUtilities.MakeRelative(_document.FileName, parentProperties.First().Keyword.Document.FileName);
                            property.Keyword.AddError(104, string.Format(Resources.Text.ValidationParentPropertyDuplicate, fileName), ErrorType.Suggestion);
                        }
                    }
                }

                // Duplicate section
                if (EditorConfigPackage.ValidationOptions.EnableDuplicateSections)
                {
                    if (_document.Sections.First(s => s.Item.Text == section.Item.Text) != section)
                        section.Item.AddError(105, string.Format(Resources.Text.ValidationDuplicateSection, section.Item.Text), ErrorType.Suggestion);
                }

                // Globbing pattern match
                if (!_globbingCache.ContainsKey(section.Item.Text))
                {
                    _globbingCache[section.Item.Text] = DoesFilesMatch(Path.GetDirectoryName(_document.FileName), section.Item.Text);
                }

                if (!_globbingCache[section.Item.Text])
                {
                    section.Item.AddError(113, string.Format("The globbing pattern \"{0}\" doesn't match any files. Consider removing the section", section.Item.Text), ErrorType.Suggestion);
                }
            }
        }

        private void ValidateProperties()
        {
            foreach (var property in _document.Properties)
            {
                // Only root property allowed
                if (property != _document.Root)
                    property.Keyword.AddError(106, Resources.Text.ValidationRootInSection, ErrorType.Error);
            }
        }

        private void ValidateProperty(Property property)
        {
            // Unknown keyword
            if (EditorConfigPackage.ValidationOptions.EnableUnknownProperties & !SchemaCatalog.TryGetProperty(property.Keyword.Text, out Keyword keyword))
            {
                property.Keyword.AddError(107, string.Format(Resources.Text.ValidateUnknownKeyword, property.Keyword.Text), ErrorType.Error);
            }

            // Missing value
            else if (property.Value == null)
            {
                property.Keyword.AddError(108, Resources.Text.ValidationMissingPropertyValue, ErrorType.Error);
            }
            // Value not in schema
            else if (EditorConfigPackage.ValidationOptions.EnableUnknownValues &&
                !keyword.Values.Any(v => v.Name.Equals(property.Value?.Text, StringComparison.OrdinalIgnoreCase)) &&
                !(int.TryParse(property.Value.Text, out int intValue) && intValue > 0))
            {
                property.Value.AddError(109, string.Format(Resources.Text.InvalidValue, property.Value.Text, keyword.Name), ErrorType.Error);
            }

            // Missing severity
            else if (property.Severity == null && property.Value.Text.Equals("true", StringComparison.OrdinalIgnoreCase) && keyword.SupportsSeverity)
            {
                property.Value.AddError(110, Resources.Text.ValidationMissingSeverity, ErrorType.Error);
            }
            else if (property.Severity != null)
            {
                // Severity not applicaple to property
                if (!keyword.SupportsSeverity)
                {
                    property.Severity.AddError(111, string.Format(Resources.Text.ValidationSeverityNotApplicable, keyword.Name), ErrorType.Error);
                }
                // Severity not in schema
                else if (!SchemaCatalog.TryGetSeverity(property.Severity.Text, out Severity severity))
                {
                    property.Severity.AddError(112, string.Format(Resources.Text.ValidationInvalidSeverity, property.Severity.Text), ErrorType.Error);
                }
            }
        }

        public static bool DoesFilesMatch(string folder, string pattern, string root = null)
        {
            var ignorePaths = new[] { "\\node_modules", "\\.git", "\\packages", "\\bower_components", "\\jspm_packages", "\\testresults", "\\.vs" };
            root = root ?? folder;
            pattern = pattern.Trim('[', ']');

            try
            {
                foreach (var file in Directory.EnumerateFiles(folder).Where(f => !ignorePaths.Any(p => folder.Contains(p))))
                {
                    string relative = file.Replace(root, "");

                    if (CheckGlobbing(relative, pattern))
                        return true;
                }

                foreach (var directory in Directory.EnumerateDirectories(folder))
                {
                    if (!ignorePaths.Any(i => directory.Contains(i)))
                    {
                        var isMatch = DoesFilesMatch(directory, pattern, root);

                        if (isMatch)
                            return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }

            return false;
        }

        private static readonly Options _options = new Options { AllowWindowsPaths = true, MatchBase = true };

        public static bool CheckGlobbing(string path, string pattern)
        {
            string p = pattern?.TrimEnd('/');

            if (!string.IsNullOrWhiteSpace(p))
            {
                return Minimatcher.Check(path, p, _options);
            }

            return false;
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            Validated = null;

            _document.Parsed -= DocumentParsedAsync;
            ValidationOptions.Saved -= DocumentParsedAsync;
        }

        public event EventHandler Validated;
    }
}
