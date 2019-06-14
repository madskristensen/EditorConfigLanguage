using System;
using System.Collections.Immutable;

namespace EditorConfig.Validation.NamingStyles
{
    internal sealed class SymbolSpecification
    {
        internal static readonly ImmutableArray<SymbolKindOrTypeKind> DefaultApplicableSymbolKindList = ImmutableArray.Create(
            SymbolKindOrTypeKind.Namespace,
            SymbolKindOrTypeKind.Class,
            SymbolKindOrTypeKind.Struct,
            SymbolKindOrTypeKind.Interface,
            SymbolKindOrTypeKind.Delegate,
            SymbolKindOrTypeKind.Enum,
            SymbolKindOrTypeKind.Module,
            SymbolKindOrTypeKind.Pointer,
            SymbolKindOrTypeKind.Property,
            SymbolKindOrTypeKind.Ordinary,
            SymbolKindOrTypeKind.LocalFunction,
            SymbolKindOrTypeKind.Field,
            SymbolKindOrTypeKind.Event,
            SymbolKindOrTypeKind.Parameter,
            SymbolKindOrTypeKind.TypeParameter,
            SymbolKindOrTypeKind.Local);

        internal static readonly ImmutableArray<Accessibility> DefaultApplicableAccessibilityList = ImmutableArray.Create(
            Accessibility.NotApplicable,
            Accessibility.Public,
            Accessibility.Internal,
            Accessibility.Private,
            Accessibility.Protected,
            Accessibility.ProtectedAndInternal,
            Accessibility.ProtectedOrInternal);

        internal static readonly ImmutableArray<ModifierKind> DefaultRequiredModifierList = ImmutableArray<ModifierKind>.Empty;

        public Guid ID { get; }
        public string Name { get; }

        public ImmutableArray<SymbolKindOrTypeKind> ApplicableSymbolKindList { get; }
        public ImmutableArray<Accessibility> ApplicableAccessibilityList { get; }
        public ImmutableArray<ModifierKind> RequiredModifierList { get; }

        public SymbolSpecification(
            Guid? id,
            string symbolSpecName,
            ImmutableArray<SymbolKindOrTypeKind> symbolKindList,
            ImmutableArray<Accessibility> accessibilityList,
            ImmutableArray<ModifierKind> modifiers)
        {
            ID = id ?? Guid.NewGuid();
            Name = symbolSpecName;
            ApplicableSymbolKindList = symbolKindList.IsDefault ? DefaultApplicableSymbolKindList : symbolKindList;
            ApplicableAccessibilityList = accessibilityList.IsDefault ? DefaultApplicableAccessibilityList : accessibilityList;
            RequiredModifierList = modifiers.IsDefault ? DefaultRequiredModifierList : modifiers;
        }
    }
}
