using System;
using System.Collections.Generic;
using System.IO;

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
    /// [$RootKey$\Languages\Language Services\EditorConfig\Schemas]
    /// "MyExtension"="$PackageFolder$\schema.json"
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
        /// Loads all custom keywords from schemas registered in the VS registry.
        /// </summary>
        /// <param name="builtInKeywordNames">Set of built-in keyword names to exclude from custom schemas (case-insensitive).</param>
        /// <returns>List of keywords from custom schemas, excluding any that conflict with built-in keywords.</returns>
        internal static IEnumerable<Keyword> LoadCustomKeywords(HashSet<string> builtInKeywordNames)
        {
            var customKeywords = new List<Keyword>();
            var seenKeywordNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string schemaPath in GetRegisteredSchemaPaths())
            {
                try
                {
                    IEnumerable<Keyword> keywords = LoadKeywordsFromFile(schemaPath);

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

                        seenKeywordNames.Add(keyword.Name);
                        customKeywords.Add(keyword);
                    }
                }
                catch (Exception ex)
                {
                    // Queue the error message to show on the UI thread
#pragma warning disable VSTHRD110 // Observe result of async calls
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        ShowSchemaLoadError(schemaPath, ex);
                    });
#pragma warning restore VSTHRD110 // Observe result of async calls
                }
            }

            return customKeywords;
        }

        /// <summary>
        /// Reads all registered schema file paths from the VS registry.
        /// </summary>
        private static IEnumerable<string> GetRegisteredSchemaPaths()
        {
            var paths = new List<string>();

            try
            {
                // VSRegistry.RegistryRoot is thread-safe and returns the correct config hive
                using (RegistryKey configRoot = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration))
                {
                    if (configRoot == null)
                    {
                        return paths;
                    }

                    using (RegistryKey schemaKey = configRoot.OpenSubKey(SchemaRegistrySubKey))
                    {
                        if (schemaKey == null)
                        {
                            return paths;
                        }

                        foreach (string valueName in schemaKey.GetValueNames())
                        {
                            object value = schemaKey.GetValue(valueName);
                            if (value is string schemaPath && !string.IsNullOrWhiteSpace(schemaPath))
                            {
                                // VS resolves $PackageFolder$ during pkgdef processing,
                                // so the registry value should already be an absolute path
                                if (File.Exists(schemaPath))
                                {
                                    paths.Add(schemaPath);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore registry access errors
            }

            return paths;
        }

        /// <summary>
        /// Loads keywords from a schema JSON file.
        /// </summary>
        private static IEnumerable<Keyword> LoadKeywordsFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            JObject obj = JObject.Parse(json);

            JToken propertiesToken = obj["properties"];
            if (propertiesToken == null)
            {
                return Array.Empty<Keyword>();
            }

            return JsonConvert.DeserializeObject<IEnumerable<Keyword>>(propertiesToken.ToString());
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
                Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
