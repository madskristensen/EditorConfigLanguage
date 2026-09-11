using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EditorConfig
{
    /// <summary>
    /// Discovers and loads custom EditorConfig schema extensions registered by other VS extensions.
    /// </summary>
    /// <remarks>
    /// Extensions can register custom schemas via pkgdef:
    /// <code>
    /// [$RootKey$\Languages\Language Services\EditorConfig\Schemas\MyExtension]
    /// "schema"="$PackageFolder$\schema.json"
    /// "moniker"="KnownMonikers.JsonScript"
    /// </code>
    /// Custom schemas follow the same format as EditorConfig.json but only the "properties" array is used.
    /// Severities cannot be added or modified by custom schemas.
    /// Built-in properties take precedence over custom ones.
    /// </remarks>
    internal static class CustomSchemaProvider
    {
        /// <summary>
        /// The registry subkey path under the VS root where custom schemas are registered.
        /// </summary>
        internal const string SchemaRegistrySubKey = @"Languages\Language Services\EditorConfig\Schemas";

        /// <summary>
        /// Loads all custom schema info from schemas registered in the VS registry.
        /// </summary>
        /// <param name="builtInKeywordNames">Set of built-in keyword names to exclude from custom schemas (case-insensitive).</param>
        /// <returns>List of custom schema info objects with keywords, excluding any that conflict with built-in keywords.</returns>
        internal static IReadOnlyList<CustomSchemaInfo> LoadCustomSchemas(HashSet<string> builtInKeywordNames)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var schemas = new List<CustomSchemaInfo>();
            var seenKeywordNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CustomSchemaRegistration registration in GetRegisteredSchemas())
            {
                try
                {
                    List<Keyword> keywords = LoadKeywordsFromFile(registration.SchemaPath);
                    IReadOnlyList<Keyword> filteredKeywords = FilterKeywords(
                        registration.ExtensionName,
                        registration.Moniker,
                        keywords,
                        builtInKeywordNames,
                        seenKeywordNames);

                    if (filteredKeywords.Count > 0)
                    {
                        schemas.Add(new CustomSchemaInfo(registration.ExtensionName, registration.Moniker, filteredKeywords));
                    }
                }
                catch (Exception ex)
                {
                    ShowSchemaLoadError(registration.SchemaPath, ex);
                }
            }

            return schemas;
        }

        /// <summary>
        /// Reads all registered schema registrations from the VS registry.
        /// </summary>
        private static IEnumerable<CustomSchemaRegistration> GetRegisteredSchemas()
        {
            var registrations = new List<CustomSchemaRegistration>();

            try
            {
                // VSRegistry.RegistryRoot is thread-safe and returns the correct config hive
                using (RegistryKey configRoot = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration))
                {
                    if (configRoot == null)
                    {
                        return registrations;
                    }

                    using (RegistryKey schemasKey = configRoot.OpenSubKey(SchemaRegistrySubKey))
                    {
                        if (schemasKey == null)
                        {
                            return registrations;
                        }

                        // Each extension has its own subkey
                        foreach (string extensionName in schemasKey.GetSubKeyNames())
                        {
                            using (RegistryKey extensionKey = schemasKey.OpenSubKey(extensionName))
                            {
                                if (extensionKey == null)
                                {
                                    continue;
                                }

                                string schemaPath = extensionKey.GetValue("schema") as string;
                                string monikerString = extensionKey.GetValue("moniker") as string;

                                if (string.IsNullOrWhiteSpace(schemaPath) || !File.Exists(schemaPath))
                                {
                                    continue;
                                }

                                ImageMoniker moniker = ParseMoniker(monikerString);

                                registrations.Add(new CustomSchemaRegistration(extensionName, schemaPath, moniker));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore registry access errors
            }

            return registrations;
        }

        /// <summary>
        /// Parses a moniker string into an ImageMoniker.
        /// Supports "KnownMonikers.Name" format or "guid:id" format.
        /// </summary>
        internal static ImageMoniker ParseMoniker(string monikerString)
        {
            if (string.IsNullOrWhiteSpace(monikerString))
            {
                return KnownMonikers.Property;
            }

            // Try KnownMonikers.Name format
            if (monikerString.StartsWith("KnownMonikers.", StringComparison.OrdinalIgnoreCase))
            {
                string monikerName = monikerString.Substring("KnownMonikers.".Length);
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase;
                PropertyInfo prop = typeof(KnownMonikers).GetProperty(monikerName, flags);
                if (prop != null)
                {
                    return (ImageMoniker)prop.GetValue(null);
                }

                FieldInfo field = typeof(KnownMonikers).GetField(monikerName, flags);
                if (field != null)
                {
                    return (ImageMoniker)field.GetValue(null);
                }
            }

            // Try guid:id format
            int colonIndex = monikerString.IndexOf(':');
            if (colonIndex > 0)
            {
                string guidPart = monikerString.Substring(0, colonIndex);
                string idPart = monikerString.Substring(colonIndex + 1);

                if (Guid.TryParse(guidPart, out Guid guid) && int.TryParse(idPart, out int id))
                {
                    return new ImageMoniker { Guid = guid, Id = id };
                }
            }

            return KnownMonikers.Property;
        }

        /// <summary>
        /// Loads keywords from a schema JSON file.
        /// </summary>
        internal static List<Keyword> LoadKeywordsFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var obj = JObject.Parse(json);

            JToken propertiesToken = obj["properties"];
            if (propertiesToken == null)
            {
                return [];
            }

            return JsonConvert.DeserializeObject<List<Keyword>>(propertiesToken.ToString());
        }

        internal static IReadOnlyList<Keyword> FilterKeywords(
            string extensionName,
            ImageMoniker moniker,
            IEnumerable<Keyword> keywords,
            ISet<string> builtInKeywordNames,
            ISet<string> seenKeywordNames)
        {
            var filteredKeywords = new List<Keyword>();

            foreach (Keyword keyword in keywords)
            {
                if (builtInKeywordNames.Contains(keyword.Name) || !seenKeywordNames.Add(keyword.Name))
                    continue;

                keyword.CustomExtensionName = extensionName;
                keyword.CustomMoniker = moniker;
                filteredKeywords.Add(keyword);
            }

            return filteredKeywords;
        }

        /// <summary>
        /// Shows an error message when a custom schema fails to load.
        /// </summary>
        private static void ShowSchemaLoadError(string schemaPath, Exception ex)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string message = string.Format(
                "Failed to load EditorConfig custom schema:\n\n{0}\n\nError: {1}",
                schemaPath,
                ex.Message);

            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                message,
                "EditorConfig Schema Error",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// Internal class to hold schema registration info from the registry.
        /// </summary>
        private sealed class CustomSchemaRegistration(string extensionName, string schemaPath, ImageMoniker moniker)
        {
            public string ExtensionName { get; } = extensionName;
            public string SchemaPath { get; } = schemaPath;
            public ImageMoniker Moniker { get; } = moniker;
        }
    }
}
