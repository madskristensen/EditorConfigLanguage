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
                ErrorCatalog.UnknownElement.Run(item, (e) =>
                {
                    e.Register();
                });
            }
        }

        private void ValidateSections()
        {
            IEnumerable<EditorConfigDocument> parents = GetAllParentDocuments();

            foreach (Section section in _document.Sections)
            {
                IEnumerable<Section> parentSections = parents.SelectMany(d => d.Sections).Where(s => s.Item.Text == section.Item.Text);

                foreach (Property property in section.Properties)
                {
                    ValidateProperty(property);

                    if (!property.IsValid)
                        continue;

                    ErrorCatalog.RootInSection.Run(property.Keyword, (e) =>
                    {
                        if (property.Keyword.Text.Is(SchemaCatalog.Root))
                        {
                            e.Register();
                        }
                    });

                    ErrorCatalog.DuplicateProperty.Run(property.Keyword, (e) =>
                    {
                        if (section.Properties.Last(p => p.Keyword.Text.Is(property.Keyword.Text)) != property)
                        {
                            e.Register();
                        }
                    });

                    ErrorCatalog.ParentDuplicateProperty.Run(property.Keyword, (e) =>
                    {
                        if (!property.Keyword.Errors.Any() && parentSections.Any())
                        {
                            IEnumerable<Property> parentProperties = parentSections.SelectMany(s => s.Properties.Where(p => p.ToString() == property.ToString()));
                            if (parentProperties.Any())
                            {
                                string fileName = PackageUtilities.MakeRelative(_document.FileName, parentProperties.First().Keyword.Document.FileName);
                                e.Register(fileName);
                            }
                        }
                    });

                    ErrorCatalog.TabWidthUnneeded.Run(property.Keyword, property.Keyword.Text.Is("tab_width"), (e) =>
                    {
                        bool hasIndentSize = section.Properties.Any(p => p.IsValid && p.Keyword.Text.Is("indent_size") && p.Value.Text.Is(property.Value.Text));
                        if (hasIndentSize)
                        {
                            e.Register();
                        }
                    });

                    ErrorCatalog.IndentSizeUnneeded.Run(property.Keyword, (e) =>
                    {
                        if (property.Keyword.Text.Is("indent_style") && property.Value.Text.Is("tab"))
                        {
                            IEnumerable<Property> indentSizes = section.Properties.Where(p => p.IsValid && p.Keyword.Text.Is("indent_size"));

                            foreach (Property indentSize in indentSizes)
                            {
                                e.Register(indentSize.Keyword);
                            }
                        }
                    });
                }

                ErrorCatalog.SectionSyntaxError.Run(section.Item, (e) =>
                {
                    if (!section.Item.Text.StartsWith("[") || !section.Item.Text.EndsWith("]"))
                    {
                        e.Register();
                    }
                });

                ErrorCatalog.SpaceInSection.Run(section.Item, (e) =>
                {
                    if (section.Item.Text.Contains(" "))
                    {
                        e.Register();
                    }
                });

                ErrorCatalog.DuplicateSection.Run(section.Item, (e) =>
                {
                    if (_document.Sections.First(s => s.Item.Text == section.Item.Text) != section)
                    {
                        e.Register(section.Item.Text);
                    }
                });

                ErrorCatalog.GlobbingNoMatch.Run(section.Item, !section.Item.HasErrors, (e) =>
                {
                    if (!_globbingCache.ContainsKey(section.Item.Text))
                    {
                        _globbingCache[section.Item.Text] = DoesFilesMatch(Path.GetDirectoryName(_document.FileName), section.Item.Text);
                    }

                    if (!_globbingCache[section.Item.Text])
                    {
                        e.Register(section.Item.Text);
                    }
                });
            }
        }

        private IEnumerable<EditorConfigDocument> GetAllParentDocuments()
        {
            if (EditorConfigPackage.ValidationOptions.EnableDuplicateFoundInParent)
            {
                EditorConfigDocument parent = _document.Parent;

                while (parent != null)
                {
                    yield return parent;
                    parent = parent.Parent;
                }
            }
        }

        private void ValidateRootProperties()
        {
            foreach (Property property in _document.Properties)
            {
                ErrorCatalog.OnlyRootAllowd.Run(property.Keyword, (e) =>
                {
                    if (property != _document.Root)
                    {
                        e.Register();
                    }
                });
            }
        }

        private void ValidateProperty(Property property)
        {
            bool hasKeyword = SchemaCatalog.TryGetKeyword(property.Keyword.Text, out Keyword keyword);

            // Unknown keyword
            ErrorCatalog.UnknownKeyword.Run(property.Keyword, !hasKeyword, (e) =>
            {
                e.Register(property.Keyword.Text);
            });

            ErrorCatalog.MissingValue.Run(property.Keyword, property.Value == null, (e) =>
            {
                e.Register();
            });

            ErrorCatalog.MissingSeverity.Run(property.Value, hasKeyword, (e) =>
            {
                if (property.Severity == null && keyword.RequiresSeverity)
                {
                    e.Register();
                }
            });

            ErrorCatalog.UnknownValue.Run(property.Value, hasKeyword, (e) =>
            {
                if (!(int.TryParse(property.Value.Text, out int intValue) && intValue > 0))
                {
                    if (keyword.SupportsMultipleValues)
                    {
                        foreach (string value in property.Value.Text?.Split(','))
                        {
                            if (!keyword.Values.Any(v => v.Name.Is(value.Trim())))
                            {
                                e.Register(keyword.Name);
                            }
                        }
                    }
                    else
                    {
                        if (!keyword.Values.Any(v => v.Name.Is(property.Value.Text)))
                        {
                            e.Register(property.Value.Text, keyword.Name);
                        }
                    }
                }
            });

            ErrorCatalog.SeverityNotApplicable.Run(property.Severity, !hasKeyword, (e) =>
            {
                if (property.Severity != null && !keyword.RequiresSeverity)
                {
                    e.Register(property.Keyword.Text);
                }
            });

            ErrorCatalog.UnknownSeverity.Run(property.Severity, property.Severity != null, (e) =>
            {
                if (!SchemaCatalog.TryGetSeverity(property.Severity.Text, out Severity severity))
                {
                    e.Register(property.Severity.Text);
                }
            });
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
                return Minimatcher.Check(path, p, _miniMatchOptions);
            }

            return false;
        }
    }
}
