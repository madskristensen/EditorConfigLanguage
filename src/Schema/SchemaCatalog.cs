using Microsoft.VisualStudio.Imaging;
using Newtonsoft.Json;
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
        private static IEnumerable<Keyword> _allProperties;

        static SchemaCatalog()
        {
            PopulateKeywords();
            PopulateSeverities();
        }

        public static IEnumerable<Keyword> Properties { get; private set; }
        public static IEnumerable<Severity> Severities { get; private set; }

        public static bool TryGetProperty(string name, out Keyword property)
        {   
            property = _allProperties.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            return property != null;
        }

        public static bool TryGetSeverity(string name, out Severity severity)
        {
            severity = Severities.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return severity != null;
        }

        private static void PopulateKeywords()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly);
            string file = Path.Combine(folder, "schema\\Keywords.json");

            _allProperties = JsonConvert.DeserializeObject<IEnumerable<Keyword>>(File.ReadAllText(file));
            Properties = _allProperties.Where(p => p.IsVisible);
        }

        private static void PopulateSeverities()
        {
            Severities = new[] {
                new Severity("none", Resources.Text.SeverityNone, KnownMonikers.None),
                new Severity("suggestion", Resources.Text.SeveritySuggestion, KnownMonikers.StatusInformation),
                new Severity("warning", Resources.Text.SeverityWarning, KnownMonikers.StatusWarning),
                new Severity("error", Resources.Text.SeverityError, KnownMonikers.StatusError)
            };
        }
    }
}
