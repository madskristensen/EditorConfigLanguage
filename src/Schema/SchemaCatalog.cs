using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private static IReadOnlyList<Keyword> _compoundKeywords;

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

        /// <summary>A list of custom schema info from registered extensions.</summary>
        internal static IReadOnlyList<CustomSchemaInfo> CustomSchemas { get; private set; }

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

            keyword = _compoundKeywords.FirstOrDefault(candidate => candidate.MatchesName(name));

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
                var obj = JObject.Parse(File.ReadAllText(file));

                Severities = JsonConvert.DeserializeObject<IEnumerable<Severity>>(obj["severities"].ToString());
                List<Keyword> builtInKeywords = JsonConvert.DeserializeObject<List<Keyword>>(obj["properties"].ToString());

                // Build set of built-in keyword names for precedence checking
                var builtInKeywordNames = new HashSet<string>(
                    builtInKeywords.Select(k => k.Name),
                    StringComparer.OrdinalIgnoreCase);

                // Load custom schemas from extensions registered in the VS registry
                CustomSchemas = CustomSchemaProvider.LoadCustomSchemas(builtInKeywordNames);

                // Collect all custom keywords from all schemas
                IEnumerable<Keyword> customKeywords = CustomSchemas.SelectMany(s => s.Keywords);

                // Merge: built-in keywords first, then custom keywords
                AllKeywords = [.. builtInKeywords, .. customKeywords];
                VisibleKeywords = AllKeywords.Where(p => p.IsVisible);

                // Build lookup dictionaries for O(1) access with case-insensitive comparison
                _keywordLookup = new Dictionary<string, Keyword>(StringComparer.OrdinalIgnoreCase);
                foreach (Keyword keyword in AllKeywords)
                {
                    _keywordLookup[keyword.Name] = keyword;

                    foreach (string alias in keyword.Aliases)
                    {
                        if (!_keywordLookup.ContainsKey(alias))
                            _keywordLookup.Add(alias, keyword);
                    }
                }

                _compoundKeywords = [.. AllKeywords.Where(keyword => keyword.Name.IndexOf('<') >= 0)];
                _severityLookup = Severities.ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
