using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EditorConfig.Validation.NamingStyles
{
    internal static class EditorConfigNamingStyleParser
    {
        public static NamingStylePreferences GetNamingStyles(IEnumerable<Property> properties)
        {
            var propertyLookup = properties.ToDictionary(property => property.Keyword.Text.Trim());

            ImmutableArray<SymbolSpecification>.Builder symbolSpecifications = ImmutableArray.CreateBuilder<SymbolSpecification>();
            ImmutableArray<NamingStyle>.Builder namingStyles = ImmutableArray.CreateBuilder<NamingStyle>();
            ImmutableArray<SerializableNamingRule>.Builder namingRules = ImmutableArray.CreateBuilder<SerializableNamingRule>();

            foreach (string title in GetRuleTitles(properties))
            {
                if (TryGetSymbolSpecification(title, propertyLookup, out SymbolSpecification symbolSpecification)
                    && TryGetNamingStyle(title, propertyLookup, out NamingStyle namingStyle)
                    && TryGetNamingRule(title, symbolSpecification, namingStyle, propertyLookup, out SerializableNamingRule namingRule))
                {
                    symbolSpecifications.Add(symbolSpecification);
                    namingStyles.Add(namingStyle);
                    namingRules.Add(namingRule);
                }
            }

            return new NamingStylePreferences(
                symbolSpecifications.ToImmutable(),
                namingStyles.ToImmutable(),
                namingRules.ToImmutable());
        }

        private static IEnumerable<string> GetRuleTitles(IEnumerable<Property> properties)
        {
            IEnumerable<string> titles = from property in properties
                                         let name = property.Keyword.Text.Trim()
                                         where name.StartsWith("dotnet_naming_rule.", StringComparison.Ordinal)
                                         let nameSplit = name.Split('.')
                                         where nameSplit.Length == 3
                                         select nameSplit[1];
            return titles.Distinct();
        }

        private static bool TryGetSymbolSpecification(string title, Dictionary<string, Property> propertyLookup, out SymbolSpecification symbolSpecification)
        {
            symbolSpecification = null;
            if (!TryGetSymbolSpecificationNameForNamingRule(out string symbolSpecificationName))
            {
                return false;
            }

            symbolSpecification = new SymbolSpecification(
                id: null,
                symbolSpecificationName,
                symbolKindList: GetSymbolsApplicableKinds(),
                accessibilityList: GetSymbolsApplicableAccessibilities(),
                modifiers: GetSymbolsRequiredModifiers());
            return true;

            // Local functions
            bool TryGetSymbolSpecificationNameForNamingRule(out string name)
            {
                if (propertyLookup.TryGetValue($"dotnet_naming_rule.{title}.symbols", out Property nameProperty))
                {
                    name = nameProperty.Value.Text.Trim();
                    return name != null;
                }

                name = null;
                return false;
            }

            ImmutableArray<SymbolKindOrTypeKind> GetSymbolsApplicableKinds()
            {
                if (propertyLookup.TryGetValue($"dotnet_naming_symbols.{symbolSpecificationName}.applicable_kinds", out Property applicableKindsProperty))
                {
                    return ParseSymbolKindList(applicableKindsProperty?.Value.Text.Trim() ?? "");
                }

                return SymbolSpecification.DefaultApplicableSymbolKindList;
            }

            ImmutableArray<SymbolKindOrTypeKind> ParseSymbolKindList(string applicableKinds)
            {
                if (applicableKinds is null)
                {
                    return ImmutableArray<SymbolKindOrTypeKind>.Empty;
                }

                if (applicableKinds == "*")
                {
                    return SymbolSpecification.DefaultApplicableSymbolKindList;
                }

                ImmutableArray<SymbolKindOrTypeKind>.Builder result = ImmutableArray.CreateBuilder<SymbolKindOrTypeKind>();
                foreach (string applicableKind in applicableKinds.Split(','))
                {
                    switch (applicableKind.Trim())
                    {
                        case "class":
                            result.Add(SymbolKindOrTypeKind.Class);
                            break;
                        case "struct":
                            result.Add(SymbolKindOrTypeKind.Struct);
                            break;
                        case "interface":
                            result.Add(SymbolKindOrTypeKind.Interface);
                            break;
                        case "enum":
                            result.Add(SymbolKindOrTypeKind.Enum);
                            break;
                        case "property":
                            result.Add(SymbolKindOrTypeKind.Property);
                            break;
                        case "method":
                            result.Add(SymbolKindOrTypeKind.Method);
                            break;
                        case "local_function":
                            result.Add(SymbolKindOrTypeKind.LocalFunction);
                            break;
                        case "field":
                            result.Add(SymbolKindOrTypeKind.Field);
                            break;
                        case "event":
                            result.Add(SymbolKindOrTypeKind.Event);
                            break;
                        case "delegate":
                            result.Add(SymbolKindOrTypeKind.Delegate);
                            break;
                        case "parameter":
                            result.Add(SymbolKindOrTypeKind.Parameter);
                            break;
                        case "type_parameter":
                            result.Add(SymbolKindOrTypeKind.TypeParameter);
                            break;
                        case "namespace":
                            result.Add(SymbolKindOrTypeKind.Namespace);
                            break;
                        case "local":
                            result.Add(SymbolKindOrTypeKind.Local);
                            break;
                        default:
                            break;
                    }
                }

                return result.ToImmutable();
            }

            ImmutableArray<Accessibility> GetSymbolsApplicableAccessibilities()
            {
                if (propertyLookup.TryGetValue($"dotnet_naming_symbols.{symbolSpecificationName}.applicable_accessibilities", out Property accessibilitiesProperty))
                {
                    return ParseAccessibilityKindList(accessibilitiesProperty?.Value.Text.Trim() ?? string.Empty);
                }

                return SymbolSpecification.DefaultApplicableAccessibilityList;
            }

            ImmutableArray<Accessibility> ParseAccessibilityKindList(string accessibilities)
            {
                if (accessibilities is null)
                {
                    return ImmutableArray<Accessibility>.Empty;
                }

                if (accessibilities == "*")
                {
                    return SymbolSpecification.DefaultApplicableAccessibilityList;
                }

                ImmutableArray<Accessibility>.Builder result = ImmutableArray.CreateBuilder<Accessibility>();
                foreach (string accessibility in accessibilities.Split(','))
                {
                    switch (accessibility.Trim())
                    {
                        case "public":
                            result.Add(Accessibility.Public);
                            break;
                        case "internal":
                        case "friend":
                            result.Add(Accessibility.Internal);
                            break;
                        case "private":
                            result.Add(Accessibility.Private);
                            break;
                        case "protected":
                            result.Add(Accessibility.Protected);
                            break;
                        case "protected_internal":
                        case "protected_friend":
                            result.Add(Accessibility.ProtectedOrInternal);
                            break;
                        case "private_protected":
                            result.Add(Accessibility.ProtectedAndInternal);
                            break;
                        case "local":
                            result.Add(Accessibility.NotApplicable);
                            break;
                        default:
                            break;
                    }
                }

                return result.ToImmutable();
            }

            ImmutableArray<ModifierKind> GetSymbolsRequiredModifiers()
            {
                if (propertyLookup.TryGetValue($"dotnet_naming_symbols.{symbolSpecificationName}.required_modifiers", out Property modifiersProperty))
                {
                    return ParseModifiers(modifiersProperty?.Value.Text.Trim() ?? string.Empty);
                }

                return ImmutableArray<ModifierKind>.Empty;
            }

            ImmutableArray<ModifierKind> ParseModifiers(string modifiers)
            {
                if (modifiers is null)
                {
                    return ImmutableArray<ModifierKind>.Empty;
                }

                var result = new List<ModifierKind>();
                foreach (string modifier in modifiers.Split(','))
                {
                    switch (modifier.Trim())
                    {
                        case "abstract":
                        case "must_inherit":
                            result.Add(ModifierKind.IsAbstract);
                            break;
                        case "async":
                            result.Add(ModifierKind.IsAsync);
                            break;
                        case "const":
                            result.Add(ModifierKind.IsConst);
                            result.Add(ModifierKind.IsReadOnly);
                            result.Add(ModifierKind.IsStatic);
                            break;
                        case "readonly":
                            result.Add(ModifierKind.IsReadOnly);
                            break;
                        case "static":
                        case "shared":
                            result.Add(ModifierKind.IsStatic);
                            break;
                        default:
                            break;
                    }
                }

                return result.Distinct().ToImmutableArray();
            }
        }

        private static bool TryGetNamingStyle(string title, Dictionary<string, Property> propertyLookup, out NamingStyle namingStyle)
        {
            namingStyle = default;
            if (!TryGetNamingStyleNameForNamingRule(out _))
            {
                return false;
            }

            namingStyle = new NamingStyle(Guid.NewGuid());
            return true;

            // Local functions
            bool TryGetNamingStyleNameForNamingRule(out string name)
            {
                if (propertyLookup.TryGetValue($"dotnet_naming_rule.{title}.style", out Property nameProperty))
                {
                    name = nameProperty.Value.Text.Trim();
                    return name != null;
                }

                name = null;
                return false;
            }
        }

        private static bool TryGetNamingRule(string title, SymbolSpecification symbolSpecification, NamingStyle namingStyle, Dictionary<string, Property> propertyLookup, out SerializableNamingRule namingRule)
        {
            if (!TryGetRuleSeverity(out ReportDiagnostic enforcementLevel))
            {
                namingRule = null;
                return false;
            }

            namingRule = new SerializableNamingRule()
            {
                Name = title,
                EnforcementLevel = enforcementLevel,
                NamingStyleID = namingStyle.ID,
                SymbolSpecificationID = symbolSpecification.ID,
            };
            return true;

            // Local functions
            bool TryGetRuleSeverity(out ReportDiagnostic severity)
            {
                if (propertyLookup.TryGetValue($"dotnet_naming_rule.{title}.severity", out Property severityProperty))
                {
                    severity = ParseEnforcementLevel(severityProperty?.Value.Text.Trim() ?? string.Empty);
                    return true;
                }

                severity = default;
                return false;
            }

            ReportDiagnostic ParseEnforcementLevel(string severity)
            {
                switch (severity.Trim())
                {
                    case "none":
                        return ReportDiagnostic.Suppress;
                    case "refactoring":
                    case "silent":
                        return ReportDiagnostic.Hidden;
                    case "suggestion":
                        return ReportDiagnostic.Info;
                    case "warning":
                        return ReportDiagnostic.Warn;
                    case "error":
                        return ReportDiagnostic.Error;
                    default:
                        return ReportDiagnostic.Hidden;
                }
            }
        }
    }
}
