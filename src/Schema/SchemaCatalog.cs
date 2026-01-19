using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EditorConfig
{
    /// <summary>Contains all the information about the properties of EditorConfig.</summary>
    public static class SchemaCatalog
    {
        /// <summary>The name of the root keyword.</summary>
        public const string Root = "root";

        // Lookup dictionaries for O(1) access
        private static Dictionary<string, Keyword> _keywordLookup;
        private static Dictionary<string, Severity> _severityLookup;

        static SchemaCatalog()
        {
            ParseJson();
        }

        /// <summary>A list of all keywords including the ones marked as hidden.</summary>
        public static IEnumerable<Keyword> AllKeywords { get; private set; }

        /// <summary>A list of all visible keywords.</summary>
        public static IEnumerable<Keyword> VisibleKeywords { get; private set; }

        /// <summary>A list of all severities.</summary>
        public static IEnumerable<Severity> Severities { get; private set; }

        /// <summary>Tries to get a keyword by name.</summary>
        public static bool TryGetKeyword(string name, out Keyword keyword)
        {
            if (name is null)
            {
                keyword = null;
                return false;
            }

            // Fast path: direct dictionary lookup (case-insensitive)
            if (_keywordLookup.TryGetValue(name, out keyword))
                return true;

            // Slow path: pattern matching for dynamic naming rules
            if (name.StartsWith("dotnet_naming_", StringComparison.OrdinalIgnoreCase) && name.IndexOf('.') > 0)
            {
                string[] parts = name.Split('.');

                if (parts.Length >= 3)
                {
                    string first = $"{parts[0]}.";
                    string last = $".{parts[parts.Length - 1]}";
                    keyword = AllKeywords.FirstOrDefault(c => c.Name.StartsWith(first, StringComparison.OrdinalIgnoreCase) && c.Name.EndsWith(last, StringComparison.OrdinalIgnoreCase));
                }
            }
            else if (name.StartsWith("dotnet_diagnostic.", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = name.Split('.');
                if (parts.Length == 3
                    && parts[0].Equals("dotnet_diagnostic", StringComparison.OrdinalIgnoreCase)
                    && parts[2].Equals("severity", StringComparison.OrdinalIgnoreCase))
                {
                    _keywordLookup.TryGetValue("dotnet_diagnostic.<rule_id>.severity", out keyword);
                }
            }
            else if (name.StartsWith("dotnet_analyzer_diagnostic.", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = name.Split('.');
                if (parts.Length == 3
                    && parts[0].Equals("dotnet_analyzer_diagnostic", StringComparison.OrdinalIgnoreCase)
                    && parts[1].StartsWith("category-", StringComparison.OrdinalIgnoreCase)
                    && parts[2].Equals("severity", StringComparison.OrdinalIgnoreCase))
                {
                    _keywordLookup.TryGetValue("dotnet_analyzer_diagnostic.category-<category>.severity", out keyword);
                }
            }
            // Support for dotnet_code_quality.<rule_id>.<option> patterns used by .NET analyzers
            else if (name.StartsWith("dotnet_code_quality.", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = name.Split('.');
                // Matches patterns like: dotnet_code_quality.CA5391.exclude_aspnet_core_mvc_controllerbase
                if (parts.Length >= 3
                    && parts[0].Equals("dotnet_code_quality", StringComparison.OrdinalIgnoreCase))
                {
                    // These are valid .NET analyzer configuration options, accept them as known
                    _keywordLookup.TryGetValue("dotnet_code_quality.<rule_id>.<option>", out keyword);
                }
            }

            return keyword != null;
        }

        /// <summary>Tries to get a severity by name.</summary>
        public static bool TryGetSeverity(string name, out Severity severity)
        {
            if (name is null)
            {
                severity = null;
                return false;
            }

            return _severityLookup.TryGetValue(name, out severity);
        }

        internal static void ParseJson(string file = null)
        {
            if (string.IsNullOrEmpty(file))
            {
                string assembly = Assembly.GetExecutingAssembly().Location;
                string folder = Path.GetDirectoryName(assembly);
                file = Path.Combine(folder, "schema\\EditorConfig.json");
            }

            if (File.Exists(file))
            {
                JObject obj = JObject.Parse(File.ReadAllText(file));

                Severities = JsonConvert.DeserializeObject<IEnumerable<Severity>>(obj["severities"].ToString());
                List<Keyword> builtInKeywords = JsonConvert.DeserializeObject<List<Keyword>>(obj["properties"].ToString());

                // Build set of built-in keyword names for precedence checking
                var builtInKeywordNames = new HashSet<string>(
                    builtInKeywords.Select(k => k.Name),
                    StringComparer.OrdinalIgnoreCase);

                // Load custom keywords from extensions registered in the VS registry
                IEnumerable<Keyword> customKeywords = CustomSchemaProvider.LoadCustomKeywords(builtInKeywordNames);

                // Merge: built-in keywords first, then custom keywords
                AllKeywords = builtInKeywords.Concat(customKeywords).ToList();
                VisibleKeywords = AllKeywords.Where(p => p.IsVisible);

                // Build lookup dictionaries for O(1) access with case-insensitive comparison
                _keywordLookup = AllKeywords.ToDictionary(k => k.Name, k => k, StringComparer.OrdinalIgnoreCase);
                _severityLookup = Severities.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
