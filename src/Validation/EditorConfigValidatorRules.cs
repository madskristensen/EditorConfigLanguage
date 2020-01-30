using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EditorConfig.Validation.NamingStyles;
using Microsoft.VisualStudio.Shell;

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

                    ErrorCatalog.UnknownStyle.Run(property.Keyword, IsDotNetNamingRuleStyle(property), (e) =>
                    {
                        if (!section.Properties.Any(p => p.Keyword.Text.Is($"dotnet_naming_style.{property.Value.Text}.capitalization")))
                        {
                            e.Register(property.Value, property.Value.Text);
                        }
                    });

                    ErrorCatalog.UnusedStyle.Run(property.Keyword, IsDotNetNamingStyle(property), (e) =>
                    {
                        string namingStyleText = GetDotNetNamingStyleText(property);
                        if (!section.Properties.Where(p => IsDotNetNamingRuleStyle(p)).Any(p => p.Value.Text.Is(namingStyleText)))
                        {
                            e.Register(property.Keyword, namingStyleText);
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

                ValidateNamingStyles(section);
            }
        }

        private void ValidateNamingStyles(Section section)
        {
            NamingStylePreferences namingStyle = EditorConfigNamingStyleParser.GetNamingStyles(section.Properties);

            IOrderedEnumerable<NamingRule> orderedRules = namingStyle.Rules.NamingRules
                .OrderBy(rule => rule, NamingRuleModifierListComparer.Instance)
                .ThenBy(rule => rule, NamingRuleAccessibilityListComparer.Instance)
                .ThenBy(rule => rule, NamingRuleSymbolListComparer.Instance)
                .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(rule => rule.Name, StringComparer.Ordinal);

            var orderedRulePreferences = new NamingStylePreferences(
                namingStyle.SymbolSpecifications,
                namingStyle.NamingStyles,
                orderedRules.Select(
                    rule => new SerializableNamingRule
                    {
                        Name = rule.Name,
                        SymbolSpecificationID = rule.SymbolSpecification.ID,
                        NamingStyleID = rule.NamingStyle.ID,
                        EnforcementLevel = rule.EnforcementLevel,
                    }).ToImmutableArray());

            var reorderedOverlappingRules = new List<(string firstRule, string secondRule)>();
            ImmutableArray<NamingRule> declaredNamingRules = namingStyle.Rules.NamingRules;
            ImmutableArray<NamingRule> orderedNamingRules = orderedRulePreferences.Rules.NamingRules;
            for (int i = 0; i < declaredNamingRules.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    NamingRule firstRule = declaredNamingRules[j];
                    NamingRule secondRule = declaredNamingRules[i];

                    // If the reordered rules are in the same relative order, no need to check the overlap
                    bool reordered = false;
                    foreach (NamingRule rule in orderedNamingRules)
                    {
                        if (rule.Name == firstRule.Name)
                        {
                            break;
                        }
                        else if (rule.Name == secondRule.Name)
                        {
                            reordered = true;
                            break;
                        }
                    }

                    if (!reordered)
                    {
                        continue;
                    }

                    // If the rules don't overlap, reordering is not a problem
                    if (!Overlaps(firstRule, secondRule))
                    {
                        continue;
                    }

                    reorderedOverlappingRules.Add((firstRule.Name, secondRule.Name));
                }
            }

            var reportedRules = new HashSet<string>();
            foreach (Property property in section.Properties)
            {
                string name = property.Keyword.Text.Trim();
                if (!name.StartsWith("dotnet_naming_rule."))
                {
                    continue;
                }

                string[] nameSplit = name.Split('.');
                if (nameSplit.Length != 3)
                {
                    continue;
                }

                string ruleName = nameSplit[1];
                if (!reportedRules.Add(ruleName))
                {
                    continue;
                }

                foreach ((string firstRule, string secondRule) in reorderedOverlappingRules)
                {
                    if (secondRule != ruleName)
                    {
                        continue;
                    }

                    ErrorCatalog.NamingRuleReordered.Run(property.Keyword, e =>
                    {
                        e.Register(secondRule, firstRule);
                    });
                }
            }
        }

        private bool Overlaps(in NamingRule x, in NamingRule y)
        {
            bool overlapAccessibility = false;
            foreach (Accessibility accessibility in x.SymbolSpecification.ApplicableAccessibilityList)
            {
                if (y.SymbolSpecification.ApplicableAccessibilityList.Contains(accessibility))
                {
                    overlapAccessibility = true;
                    break;
                }
            }

            if (!overlapAccessibility)
            {
                return false;
            }

            bool overlapSymbols = false;
            foreach (SymbolKindOrTypeKind symbolKind in x.SymbolSpecification.ApplicableSymbolKindList)
            {
                if (y.SymbolSpecification.ApplicableSymbolKindList.Contains(symbolKind))
                {
                    overlapSymbols = true;
                    break;
                }
            }

            if (!overlapSymbols)
            {
                return false;
            }

            if (x.SymbolSpecification.RequiredModifierList.IsEmpty || y.SymbolSpecification.RequiredModifierList.IsEmpty)
            {
                // Modifiers are the last check. If either is empty, it matches all so the rules must overlap.
                return true;
            }

            foreach (ModifierKind modifier in x.SymbolSpecification.RequiredModifierList)
            {
                switch (modifier)
                {
                    case ModifierKind.IsAbstract:
                        if (y.SymbolSpecification.RequiredModifierList.Contains(ModifierKind.IsStatic)
                            || y.SymbolSpecification.RequiredModifierList.Contains(ModifierKind.IsConst))
                        {
                            return false;
                        }

                        break;

                    case ModifierKind.IsStatic:
                        if (y.SymbolSpecification.RequiredModifierList.Contains(ModifierKind.IsAbstract))
                        {
                            return false;
                        }

                        break;

                    case ModifierKind.IsAsync:
                        break;

                    case ModifierKind.IsReadOnly:
                        break;

                    case ModifierKind.IsConst:
                        if (y.SymbolSpecification.RequiredModifierList.Contains(ModifierKind.IsAbstract))
                        {
                            return false;
                        }

                        break;

                    default:
                        break;
                }
            }

            return true;
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
                        foreach (string value in property.Value.Text?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!keyword.Values.Any(v => v.Name.Is(value.Trim())))
                            {
                                e.Register(value, keyword.Name);
                            }
                        }
                    }
                    else if (!keyword.Values.Any(v => Regex.IsMatch(v.Name, "<.+>")))
                    {
                        if (keyword.Values.Count() > 0 && !keyword.Values.Any(v => v.Name.Is(property.Value.Text)))
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

            var matcher = AnalyzerConfig.TryCreateSectionNameMatcher(pattern);
            if (matcher is null)
                return false;

            return DoesFilesMatch(folder, matcher.Value, root);
        }

        private static bool DoesFilesMatch(string folder, AnalyzerConfig.SectionNameMatcher matcher, string root)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(folder).Where(f => !_ignorePaths.Any(p => folder.Contains(p))))
                {
                    string relative = file.Replace(root, string.Empty);

                    if (CheckGlobbing(relative.Replace('\\', '/'), matcher))
                        return true;
                }

                foreach (string directory in Directory.EnumerateDirectories(folder))
                {
                    if (!_ignorePaths.Any(i => directory.Contains(i)) && DoesFilesMatch(directory, matcher, root))
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

        private static bool CheckGlobbing(string path, AnalyzerConfig.SectionNameMatcher matcher)
            => matcher.IsMatch(path);

        private static bool IsDotNetNamingRuleStyle(Property property) 
            => property.Keyword.Text.StartsWith("dotnet_naming_rule.", StringComparison.Ordinal) && 
               property.Keyword.Text.EndsWith(".style", StringComparison.Ordinal);

        private static bool IsDotNetNamingStyle(Property property)
            => property.Keyword.Text.StartsWith("dotnet_naming_style.");

        private static string GetDotNetNamingStyleText(Property property)
            => property.Keyword.Text.Split('.')[1];

        private abstract class NamingRuleSubsetComparer : IComparer<NamingRule>
        {
            protected NamingRuleSubsetComparer()
            {
            }

            public int Compare(NamingRule x, NamingRule y)
            {
                bool firstIsSubset = FirstIsSubset(in x, in y);
                bool secondIsSubset = FirstIsSubset(in y, in x);
                if (firstIsSubset)
                {
                    return secondIsSubset ? 0 : -1;
                }
                else
                {
                    return secondIsSubset ? 1 : 0;
                }
            }

            protected abstract bool FirstIsSubset(in NamingRule x, in NamingRule y);
        }

        private sealed class NamingRuleAccessibilityListComparer : NamingRuleSubsetComparer
        {
            internal static readonly NamingRuleAccessibilityListComparer Instance = new NamingRuleAccessibilityListComparer();

            private NamingRuleAccessibilityListComparer()
            {
            }

            protected override bool FirstIsSubset(in NamingRule x, in NamingRule y)
            {
                foreach (Accessibility accessibility in x.SymbolSpecification.ApplicableAccessibilityList)
                {
                    if (!y.SymbolSpecification.ApplicableAccessibilityList.Contains(accessibility))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private sealed class NamingRuleModifierListComparer : NamingRuleSubsetComparer
        {
            internal static readonly NamingRuleModifierListComparer Instance = new NamingRuleModifierListComparer();

            private NamingRuleModifierListComparer()
            {
            }

            protected override bool FirstIsSubset(in NamingRule x, in NamingRule y)
            {
                // Since modifiers are "match all", a subset of symbols is matched by a superset of modifiers
                foreach (ModifierKind modifier in y.SymbolSpecification.RequiredModifierList)
                {
                    if (!x.SymbolSpecification.RequiredModifierList.Contains(modifier))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private sealed class NamingRuleSymbolListComparer : NamingRuleSubsetComparer
        {
            internal static readonly NamingRuleSymbolListComparer Instance = new NamingRuleSymbolListComparer();

            private NamingRuleSymbolListComparer()
            {
            }

            protected override bool FirstIsSubset(in NamingRule x, in NamingRule y)
            {
                foreach (SymbolKindOrTypeKind symbolKind in x.SymbolSpecification.ApplicableSymbolKindList)
                {
                    if (!y.SymbolSpecification.ApplicableSymbolKindList.Contains(symbolKind))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
