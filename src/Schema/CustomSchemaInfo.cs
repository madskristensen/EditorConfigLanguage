using System.Collections.Generic;

using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    /// <summary>
    /// Contains metadata about a custom EditorConfig schema registered by another VS extension.
    /// </summary>
    internal sealed class CustomSchemaInfo(string extensionName, ImageMoniker moniker, IReadOnlyList<Keyword> keywords)
    {
        /// <summary>
        /// The name of the extension that registered this schema (used as filter name).
        /// </summary>
        public string ExtensionName { get; } = extensionName;

        /// <summary>
        /// The image moniker to use for keywords from this extension.
        /// </summary>
        public ImageMoniker Moniker { get; } = moniker;

        /// <summary>
        /// The keywords provided by this extension.
        /// </summary>
        public IReadOnlyList<Keyword> Keywords { get; } = keywords;
    }
}
