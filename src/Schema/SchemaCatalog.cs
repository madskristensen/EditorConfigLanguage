using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EditorConfig
{
    public static class SchemaCatalog
    {
        public const string Root = "root";
        private static IEnumerable<Keyword> _allKeywords;

        static SchemaCatalog()
        {
            ParseJson();
        }

        public static IEnumerable<Keyword> Keywords { get; private set; }
        public static IEnumerable<Severity> Severities { get; private set; }

        public static bool TryGetKeyword(string name, out Keyword keyword)
        {
            keyword = _allKeywords.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            return keyword != null;
        }

        public static bool TryGetSeverity(string name, out Severity severity)
        {
            severity = Severities.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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
