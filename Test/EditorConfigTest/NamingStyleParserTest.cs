using System.Collections.Generic;
using System.Linq;

using EditorConfig;
using EditorConfig.Validation.NamingStyles;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;

namespace EditorConfigTest
{
    [TestClass]
    public class NamingStyleParserTest
    {
        [TestMethod]
        public void GetNamingStyles_ParsesSymbolFilters()
        {
            NamingStylePreferences result = ParseRule(
                CreateProperty("dotnet_naming_symbols.symbols.applicable_kinds", "class, method, invalid"),
                CreateProperty("dotnet_naming_symbols.symbols.applicable_accessibilities", "public, friend, protected_friend, local, invalid"),
                CreateProperty("dotnet_naming_symbols.symbols.required_modifiers", "const, async, shared, invalid"));

            SymbolSpecification symbols = result.SymbolSpecifications.Single();

            CollectionAssert.AreEquivalent(
                new[] { SymbolKindOrTypeKind.Class, SymbolKindOrTypeKind.Method },
                symbols.ApplicableSymbolKindList.ToArray());
            CollectionAssert.AreEquivalent(
                new[] { Accessibility.Public, Accessibility.Internal, Accessibility.ProtectedOrInternal, Accessibility.NotApplicable },
                symbols.ApplicableAccessibilityList.ToArray());
            CollectionAssert.AreEquivalent(
                new[] { ModifierKind.IsConst, ModifierKind.IsReadOnly, ModifierKind.IsStatic, ModifierKind.IsAsync },
                symbols.RequiredModifierList.ToArray());
        }

        [TestMethod]
        public void GetNamingStyles_WildcardsUseDefaultSymbolFilters()
        {
            NamingStylePreferences result = ParseRule(
                CreateProperty("dotnet_naming_symbols.symbols.applicable_kinds", "*"),
                CreateProperty("dotnet_naming_symbols.symbols.applicable_accessibilities", "*"));

            SymbolSpecification symbols = result.SymbolSpecifications.Single();

            CollectionAssert.AreEqual(SymbolSpecification.DefaultApplicableSymbolKindList.ToArray(), symbols.ApplicableSymbolKindList.ToArray());
            CollectionAssert.AreEqual(SymbolSpecification.DefaultApplicableAccessibilityList.ToArray(), symbols.ApplicableAccessibilityList.ToArray());
        }

        [TestMethod]
        [DataRow("none", 5)]
        [DataRow("silent", 4)]
        [DataRow("refactoring", 4)]
        [DataRow("suggestion", 3)]
        [DataRow("warning", 2)]
        [DataRow("error", 1)]
        [DataRow("future_value", 4)]
        public void GetNamingStyles_MapsSeverities(string value, int expected)
        {
            NamingStylePreferences result = ParseRule(
                CreateProperty("dotnet_naming_rule.rule.severity", value),
                includeDefaultSeverity: false);

            Assert.AreEqual((ReportDiagnostic)expected, result.NamingRules.Single().EnforcementLevel);
        }

        [TestMethod]
        public void GetNamingStyles_IncompleteRuleIsSkipped()
        {
            NamingStylePreferences result = EditorConfigNamingStyleParser.GetNamingStyles(new[]
            {
                CreateProperty("dotnet_naming_rule.rule.symbols", "symbols"),
                CreateProperty("dotnet_naming_rule.rule.severity", "warning"),
            });

            Assert.IsEmpty(result.NamingRules);
            Assert.IsEmpty(result.SymbolSpecifications);
            Assert.IsEmpty(result.NamingStyles);
        }

        [TestMethod]
        public void GetNamingStyles_DuplicatePropertiesUseLastValue()
        {
            NamingStylePreferences result = ParseRule(
                CreateProperty("DOTNET_NAMING_RULE.RULE.SEVERITY", "error"),
                CreateProperty("dotnet_naming_rule.rule.severity", "warning"),
                includeDefaultSeverity: false);

            Assert.AreEqual(ReportDiagnostic.Warn, result.NamingRules.Single().EnforcementLevel);
        }

        private static NamingStylePreferences ParseRule(Property additional, bool includeDefaultSeverity = true)
            => ParseRule(new[] { additional }, includeDefaultSeverity);

        private static NamingStylePreferences ParseRule(Property first, Property second, bool includeDefaultSeverity = true)
            => ParseRule(new[] { first, second }, includeDefaultSeverity);

        private static NamingStylePreferences ParseRule(Property first, Property second, Property third)
            => ParseRule(new[] { first, second, third }, includeDefaultSeverity: true);

        private static NamingStylePreferences ParseRule(IEnumerable<Property> additional, bool includeDefaultSeverity)
        {
            var properties = new List<Property>
            {
                CreateProperty("dotnet_naming_rule.rule.symbols", "symbols"),
                CreateProperty("dotnet_naming_rule.rule.style", "style"),
            };

            if (includeDefaultSeverity)
                properties.Add(CreateProperty("dotnet_naming_rule.rule.severity", "warning"));

            properties.AddRange(additional);
            return EditorConfigNamingStyleParser.GetNamingStyles(properties);
        }

        private static Property CreateProperty(string name, string value)
        {
            var keyword = new ParseItem(null, ItemType.Keyword, new Span(0, name.Length), name);
            var valueItem = new ParseItem(null, ItemType.Value, new Span(name.Length + 3, value.Length), value);
            return new Property(keyword) { Value = valueItem };
        }
    }
}
