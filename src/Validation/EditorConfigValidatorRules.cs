using Microsoft.VisualStudio.Shell;
using Minimatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EditorConfig
{
    partial class EditorConfigValidator
    {
        private void ValidateUnknown()
        {
            foreach (ParseItem item in _document.ParseItems.Where(i => i.ItemType == ItemType.Unknown))
            {
                ErrorCatalog.UnknownElement.Register(item);
            }
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
                        ErrorCatalog.RootInSection.Register(property.Keyword);
                    }

                    // Duplicate property
                    if (EditorConfigPackage.ValidationOptions.EnableDuplicateProperties)
                    {
                        if (section.Properties.Last(p => p.Keyword.Text.Is(property.Keyword.Text)) != property)
                        {
                            ErrorCatalog.DuplicateProperty.Register(property.Keyword);
                        }
                    }

                    // Parent duplicate
                    if (EditorConfigPackage.ValidationOptions.EnableDuplicateFoundInParent && !property.Keyword.Errors.Any() && parentSections.Any())
                    {
                        IEnumerable<Property> parentProperties = parentSections.SelectMany(s => s.Properties.Where(p => p.ToString() == property.ToString()));
                        if (parentProperties.Any())
                        {
                            string fileName = PackageUtilities.MakeRelative(_document.FileName, parentProperties.First().Keyword.Document.FileName);
                            ErrorCatalog.ParentDuplicateProperty.Register(property.Keyword, fileName);
                        }
                    }

                    // tab_width should be different than indent_size
                    if (property.Keyword.Text.Is("tab_width"))
                    {
                        bool hasIndentSize = section.Properties.Any(p => p.IsValid && p.Keyword.Text.Is("indent_size") && p.Value.Text.Is(property.Value.Text));
                        if (hasIndentSize)
                        {
                            ErrorCatalog.TabWidthUnneeded.Register(property.Keyword);
                        }
                    }

                    // Don't set indent_size when indent_style is set to tab
                    if (property.Keyword.Text.Is("indent_style") && property.Value.Text.Is("tab"))
                    {
                        IEnumerable<Property> indentSizes = section.Properties.Where(p => p.IsValid && p.Keyword.Text.Is("indent_size"));

                        foreach (Property indentSize in indentSizes)
                        {
                            ErrorCatalog.IndentSizeUnneeded.Register(indentSize.Keyword);
                        }
                    }
                }

                // Syntax error
                if (!section.Item.Text.StartsWith("[") || !section.Item.Text.EndsWith("]"))
                {
                    ErrorCatalog.SectionSyntaxError.Register(section.Item);
                }

                // Space in pattern
                else if (!EditorConfigPackage.ValidationOptions.AllowSpacesInSections && section.Item.Text.Contains(" "))
                {
                    ErrorCatalog.SpaceInSection.Register(section.Item);
                }

                // Duplicate section
                else if (EditorConfigPackage.ValidationOptions.EnableDuplicateSections)
                {
                    if (_document.Sections.First(s => s.Item.Text == section.Item.Text) != section)
                    {
                        ErrorCatalog.DuplicateSection.Register(section.Item, section.Item.Text);
                    }
                }

                // Globbing pattern match
                else if (EditorConfigPackage.ValidationOptions.EnableGlobbingMatcher && !section.Item.HasErrors)
                {
                    if (!_globbingCache.ContainsKey(section.Item.Text))
                    {
                        _globbingCache[section.Item.Text] = DoesFilesMatch(Path.GetDirectoryName(_document.FileName), section.Item.Text);
                    }

                    if (!_globbingCache[section.Item.Text])
                    {
                        ErrorCatalog.GlobbingNoMatch.Register(section.Item);
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
                // Only "root" property allowed
                if (property != _document.Root)
                {
                    ErrorCatalog.OnlyRootAllowd.Register(property.Keyword);
                }
            }
        }

        private void ValidateProperty(Property property)
        {
            // Unknown keyword
            if (EditorConfigPackage.ValidationOptions.EnableUnknownProperties & !SchemaCatalog.TryGetKeyword(property.Keyword.Text, out Keyword keyword))
            {
                ErrorCatalog.UnknownKeyword.Register(property.Keyword, property.Keyword.Text);
            }

            // Missing value
            else if (property.Value == null)
            {
                ErrorCatalog.MissingValue.Register(property.Keyword);
            }

            // Missing severity
            else if (property.Severity == null && keyword.RequiresSeverity)
            {
                ErrorCatalog.MissingSeverity.Register(property.Value);
            }

            // Value not in schema
            else if (EditorConfigPackage.ValidationOptions.EnableUnknownValues && !(int.TryParse(property.Value.Text, out int intValue) && intValue > 0))
            {
                if (keyword.SupportsMultipleValues)
                {
                    foreach (string value in property.Value.Text?.Split(','))
                    {
                        if (!keyword.Values.Any(v => v.Name.Is(value.Trim())))
                        {
                            ErrorCatalog.UnknownValue.Register(property.Value, keyword.Name);
                        }
                    }
                }
                else
                {
                    if (!keyword.Values.Any(v => v.Name.Is(property.Value.Text)))
                    {
                        ErrorCatalog.UnknownValue.Register(property.Value, property.Value.Text, keyword.Name);
                    }
                }
            }

            if (property.Severity != null)
            {
                // Severity not applicaple to property
                if (keyword == null || !keyword.RequiresSeverity)
                {
                    ErrorCatalog.SeverityNotApplicable.Register(property.Severity, property.Keyword.Text);
                }
                // Severity not in schema
                else if (!SchemaCatalog.TryGetSeverity(property.Severity.Text, out Severity severity))
                {
                    ErrorCatalog.UnknownSeverity.Register(property.Severity, property.Severity.Text);
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
    }
}
