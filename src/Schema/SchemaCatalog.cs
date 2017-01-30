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

        private static IEnumerable<Keyword> _allKeywords;

        static SchemaCatalog()
        {
            ParseJson();
        }

        /// <summary>A list of all visible keywords.</summary>
        public static IEnumerable<Keyword> Keywords { get; private set; }

        /// <summary>A list of all severities.</summary>
        public static IEnumerable<Severity> Severities { get; private set; }

        /// <summary>Tries to get a keyword by name.</summary>
        public static bool TryGetKeyword(string name, out Keyword keyword)
        {
            keyword = _allKeywords.FirstOrDefault(c => c.Name.Is(name));

            return keyword != null;
        }

        /// <summary>Tries to get a severity by name.</summary>
        public static bool TryGetSeverity(string name, out Severity severity)
        {
            severity = Severities.FirstOrDefault(s => s.Name.Is(name));
            return severity != null;
        }

        private static void ParseJson()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly);
            string file = Path.Combine(folder, "schema\\EditorConfig.json");

            var obj = JObject.Parse(File.ReadAllText(file));

            Severities = JsonConvert.DeserializeObject<IEnumerable<Severity>>(obj["severities"].ToString());
            _allKeywords = JsonConvert.DeserializeObject<IEnumerable<Keyword>>(obj["properties"].ToString());
            Keywords = _allKeywords.Where(p => p.IsVisible);
        }
    }
}
