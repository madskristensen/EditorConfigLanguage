namespace EditorConfig.Validation.NamingStyles
{
    internal enum SymbolKindOrTypeKind
    {
        #region Symbol kinds

        Namespace,
        Event,
        Field,
        Local,
        Method,
        Parameter,
        Property,

        #endregion

        #region Type kinds

        Class,
        Delegate,
        Enum,
        Interface,
        Module,
        Pointer,
        Struct,
        TypeParameter,

        #endregion

        #region Method kinds

        Ordinary,
        LocalFunction,

        #endregion
    }
}
