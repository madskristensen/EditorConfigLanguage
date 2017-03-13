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
            keyword = AllKeywords.FirstOrDefault(c => c.Name.Is(name));

            if (keyword == null && name != null && name.StartsWith("dotnet_naming_", StringComparison.OrdinalIgnoreCase) && name.IndexOf('.') > 0)
            {
                string[] parts = name.Split('.');

                if (parts.Length >= 3)
                {
                    string first = $"{parts[0]}.";
                    string last = $".{parts[parts.Length - 1]}";
                    keyword = AllKeywords.FirstOrDefault(c => c.Name.StartsWith(first, StringComparison.OrdinalIgnoreCase) && c.Name.EndsWith(last, StringComparison.OrdinalIgnoreCase));
                }
            }

            return keyword != null;
        }

        /// <summary>Tries to get a severity by name.</summary>
        public static bool TryGetSeverity(string name, out Severity severity)
        {
            severity = Severities.FirstOrDefault(s => s.Name.Is(name));
            return severity != null;
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
                AllKeywords = JsonConvert.DeserializeObject<IEnumerable<Keyword>>(obj["properties"].ToString());
                VisibleKeywords = AllKeywords.Where(p => p.IsVisible);
            }
        }
    }
}
