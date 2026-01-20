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
            var schemas = new List<CustomSchemaInfo>();
            var seenKeywordNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CustomSchemaRegistration registration in GetRegisteredSchemas())
            {
                try
                {
                    List<Keyword> keywords = LoadKeywordsFromFile(registration.SchemaPath);
                    var filteredKeywords = new List<Keyword>();

                    foreach (Keyword keyword in keywords)
                    {
                        // Skip if this keyword exists in built-in schema
                        if (builtInKeywordNames.Contains(keyword.Name))
                        {
                            continue;
                        }

                        // Skip if we've already seen this keyword from another custom schema
                        if (seenKeywordNames.Contains(keyword.Name))
                        {
                            continue;
                        }

                        // Set the custom extension info on the keyword
                        keyword.CustomExtensionName = registration.ExtensionName;
                        keyword.CustomMoniker = registration.Moniker;

                        seenKeywordNames.Add(keyword.Name);
                        filteredKeywords.Add(keyword);
                    }

                    if (filteredKeywords.Count > 0)
                    {
                        schemas.Add(new CustomSchemaInfo(registration.ExtensionName, registration.Moniker, filteredKeywords));
                    }
                }
                catch (Exception ex)
                {
                    // Queue the error message to show on the UI thread
#pragma warning disable VSTHRD110 // Observe result of async calls
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        ShowSchemaLoadError(registration.SchemaPath, ex);
                    });
#pragma warning restore VSTHRD110 // Observe result of async calls
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
        private static ImageMoniker ParseMoniker(string monikerString)
        {
            if (string.IsNullOrWhiteSpace(monikerString))
            {
                return KnownMonikers.Property;
            }

            // Try KnownMonikers.Name format
            if (monikerString.StartsWith("KnownMonikers.", StringComparison.OrdinalIgnoreCase))
            {
                string monikerName = monikerString.Substring("KnownMonikers.".Length);
                PropertyInfo prop = typeof(KnownMonikers).GetProperty(monikerName, BindingFlags.Public | BindingFlags.Static);
                if (prop != null && prop.PropertyType == typeof(ImageMoniker))
                {
                    return (ImageMoniker)prop.GetValue(null);
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
        private static List<Keyword> LoadKeywordsFromFile(string filePath)
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
