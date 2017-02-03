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
        private const int _validationDelay = 500;
        private static string[] _ignorePaths = { "\\node_modules", "\\.git", "\\packages", "\\bower_components", "\\jspm_packages", "\\testresults", "\\.vs" };

        private EditorConfigDocument _document;
        private DateTime _lastRequestForValidation;
        private Timer _timer;
        private bool _hasChanged, _validating;
        private bool _prevEnabled = EditorConfigPackage.ValidationOptions != null ? EditorConfigPackage.ValidationOptions.EnableValidation : true;
        private Dictionary<string, bool> _globbingCache = new Dictionary<string, bool>();
        private static readonly Options _options = new Options { AllowWindowsPaths = true, MatchBase = true };

        private EditorConfigValidator(EditorConfigDocument document)
        {
            _document = document;
            _document.Parsed += DocumentParsedAsync;

            if (_prevEnabled)
                ValidateAsync().ConfigureAwait(false);

            ValidationOptions.Saved += DocumentParsedAsync;
        }

        /// <summary>Gets or creates an instace of the validator and stores it in the text buffer properties.</summary>
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

        /// <summary>Schedules an async validation run.</summary>
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
            foreach (ParseItem item in _document.ParseItems.Where(i => i.Errors.Any()))
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
                    foreach (ParseItem item in _document.ParseItems.Where(i => i.ItemType == ItemType.Unknown))
                    {
                        ValidateUnknown(item);
                    }

                    ValidateRootProperties();
                    ValidateSections();

                    foreach (Property property in _document.Properties)
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
            item.AddError(PredefinedErrors.UnknownElement(item));
        }

        private void ValidateSections()
        {
            List<EditorConfigDocument> parents = GetAllParentDocuments();

            foreach (Section section in _document.Sections)
            {
                IEnumerable<Section> parentSections = parents.SelectMany(d => d.Sections).Where(s => s.Item.Text == section.Item.Text);

                foreach (Property property in section.Properties)
                {
                    ValidateProperty(property);

                    if (!property.IsValid)
                        continue;

                    // Root in section
                    if (property.Keyword.Text.Is(SchemaCatalog.Root))
                    {
                        PredefinedErrors.RootInSection(property.Keyword);
                    }

                    // Duplicate property
                    if (EditorConfigPackage.ValidationOptions.EnableDuplicateProperties)
                    {
                        if (section.Properties.Last(p => p.Keyword.Text.Is(property.Keyword.Text)) != property)
                        {
                            PredefinedErrors.DuplicateProperty(property.Keyword);
                        }
                    }

                    // Parent duplicate
                    if (EditorConfigPackage.ValidationOptions.EnableDuplicateFoundInParent && !property.Keyword.Errors.Any() && parentSections.Any())
                    {
                        IEnumerable<Property> parentProperties = parentSections.SelectMany(s => s.Properties.Where(p => p.ToString() == property.ToString()));
                        if (parentProperties.Any())
                        {
                            string fileName = PackageUtilities.MakeRelative(_document.FileName, parentProperties.First().Keyword.Document.FileName);
                            PredefinedErrors.ParentDuplicateProperty(property.Keyword, fileName);
                        }
                    }

                    // tab_width should be different than indent_size
                    if (property.Keyword.Text.Is("tab_width"))
                    {
                        bool hasIndentSize = section.Properties.Any(p => p.IsValid && p.Keyword.Text.Is("indent_size") && p.Value.Text.Is(property.Value.Text));
                        if (hasIndentSize)
                            PredefinedErrors.TabWidthUnneeded(property.Keyword);
                    }

                    // Don't set indent_size when indent_style is set to tab
                    if (property.Keyword.Text.Is("indent_style") && property.Value.Text.Is("tab"))
                    {
                        IEnumerable<Property> indentSizes = section.Properties.Where(p => p.IsValid && p.Keyword.Text.Is("indent_size"));

                        foreach (Property indentSize in indentSizes)
                        {
                            PredefinedErrors.IndentSizeUnneeded(indentSize.Keyword);
                        }
                    }
                }

                // Syntax error
                if (!section.Item.Text.StartsWith("[") || !section.Item.Text.EndsWith("]"))
                {
                    PredefinedErrors.SectionSyntaxError(section.Item);
                }

                // Duplicate section
                if (EditorConfigPackage.ValidationOptions.EnableDuplicateSections)
                {
                    if (_document.Sections.First(s => s.Item.Text == section.Item.Text) != section)
                        PredefinedErrors.DuplicateSection(section.Item);
                }

                // Globbing pattern match
                if (EditorConfigPackage.ValidationOptions.EnableGlobbingMatcher && !section.Item.HasErrors)
                {
                    if (!_globbingCache.ContainsKey(section.Item.Text))
                    {
                        _globbingCache[section.Item.Text] = DoesFilesMatch(Path.GetDirectoryName(_document.FileName), section.Item.Text);
                    }

                    if (!_globbingCache[section.Item.Text])
                    {
                        PredefinedErrors.GlobbingNoMatch(section.Item);
                    }
                }
            }
        }

        private List<EditorConfigDocument> GetAllParentDocuments()
        {
            var parents = new List<EditorConfigDocument>();

            if (EditorConfigPackage.ValidationOptions.EnableDuplicateFoundInParent)
            {
                EditorConfigDocument parent = _document.Parent;
                while (parent != null)
                {
                    parents.Add(parent);
                    parent = parent.Parent;
                }
            }

            return parents;
        }

        private void ValidateRootProperties()
        {
            foreach (Property property in _document.Properties)
            {
                // Only root property allowed
                if (property != _document.Root)
                    PredefinedErrors.OnlyRootAllowed(property.Keyword);
            }
        }

        private void ValidateProperty(Property property)
        {
            // Unknown keyword
            if (EditorConfigPackage.ValidationOptions.EnableUnknownProperties & !SchemaCatalog.TryGetKeyword(property.Keyword.Text, out Keyword keyword))
            {
                PredefinedErrors.UnknownKeyword(property.Keyword);
            }

            // Missing value
            else if (property.Value == null)
            {
                PredefinedErrors.MissingValue(property.Keyword);
            }

            // Missing severity
            else if (property.Severity == null && keyword.RequiresSeverity)
            {
                PredefinedErrors.MissingSeverity(property.Value);
            }

            // Value not in schema
            else if (EditorConfigPackage.ValidationOptions.EnableUnknownValues && !(int.TryParse(property.Value.Text, out int intValue) && intValue > 0))
            {
                if (keyword.SupportsMultipleValues)
                {
                    foreach (string value in property.Value.Text?.Split(','))
                    {
                        if (!keyword.Values.Any(v => v.Name.Is(value.Trim())))
                            PredefinedErrors.UnknownValue(property.Value, keyword.Name);
                    }
                }
                else
                {
                    if (!keyword.Values.Any(v => v.Name.Is(property.Value.Text)))
                        PredefinedErrors.UnknownValue(property.Value, keyword.Name);
                }
            }

            else if (property.Severity != null)
            {
                // Severity not applicaple to property
                if (!keyword.RequiresSeverity)
                {
                    PredefinedErrors.SeverityNotApplicable(property.Severity, keyword.Name);
                }
                // Severity not in schema
                else if (!SchemaCatalog.TryGetSeverity(property.Severity.Text, out Severity severity))
                {
                    PredefinedErrors.UnknownSeverity(property.Severity);
                }
            }
        }

        private static bool DoesFilesMatch(string folder, string pattern, string root = null)
        {
            root = root ?? folder;
            pattern = pattern.Trim('[', ']');

            // No reason to check file system since this will match at least the .editorconfig file itself
            if (pattern.Equals("*"))
                return true;

            try
            {
                foreach (string file in Directory.EnumerateFiles(folder).Where(f => !_ignorePaths.Any(p => folder.Contains(p))))
                {
                    string relative = file.Replace(root, string.Empty).TrimStart('\\');

                    if (CheckGlobbing(relative, pattern))
                        return true;
                }

                foreach (string directory in Directory.EnumerateDirectories(folder))
                {
                    if (!_ignorePaths.Any(i => directory.Contains(i)) && DoesFilesMatch(directory, pattern, root))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException("GlobbingValidator", ex);
                return true;
            }

            return false;
        }

        private static bool CheckGlobbing(string path, string pattern)
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
