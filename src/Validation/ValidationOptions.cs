using System;
using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.VisualStudio.Shell;

namespace EditorConfig
{
    public class ValidationOptions : DialogPage
    {
        // General
        private const string _general = "General";

        [Category(_general)]
        [DisplayName("Enable validation")]
        [Description("This will enable the validator to run on the document.")]
        [DefaultValue(true)]
        public bool EnableValidation { get; set; } = true;

        [Category(_general)]
        [DisplayName("Display errors as warnings")]
        [Description("This will make errors found in the document show up as warnings in the Error List.")]
        [DefaultValue(true)]
        public bool ShowErrorsAsWarnings { get; set; } = true;

        // Rules
        private const string _rules = "Rules";

        [Category(_rules)]
        [DisplayName("Validate unknown properties")]
        [Description("This will show errors for unknown properties. It is a good way to catch typos.")]
        [DefaultValue(true)]
        public bool EnableUnknownProperties { get; set; } = true;

        private string _ignoredPrefixes = "resharper_, idea_, roslynator_, ij_";
        private string[] _cachedPrefixes;

        [Category(_rules)]
        [DisplayName("Ignored property prefixes")]
        [Description("Comma-separated list of property prefixes to ignore during validation (e.g., resharper_, idea_, roslynator_).")]
        [DefaultValue("resharper_, idea_, roslynator_, ij_")]
        public string IgnoredPrefixes
        {
            get => _ignoredPrefixes;
            set
            {
                _ignoredPrefixes = value;
                _cachedPrefixes = null; // Invalidate cache when value changes
            }
        }

        [Category(_rules)]
        [DisplayName("Validate unknown values")]
        [Description("This will show errors for unknown property values.")]
        [DefaultValue(true)]
        public bool EnableUnknownValues { get; set; } = true;

        [Category(_rules)]
        [DisplayName("Validate duplicate sections")]
        [Description("This will show errors when a section has already been defined earlier in the document.")]
        [DefaultValue(true)]
        public bool EnableDuplicateSections { get; set; } = true;

        [Category(_rules)]
        [DisplayName("Validate duplicate properties")]
        [Description("This will show errors when a property has already been defined earlier in the section.")]
        [DefaultValue(true)]
        public bool EnableDuplicateProperties { get; set; } = true;

        [Category(_rules)]
        [DisplayName("Validate parent overrides")]
        [Description("Show an error if a property was also defined in a parent document with the same value and severity.")]
        [DefaultValue(true)]
        public bool EnableDuplicateFoundInParent { get; set; } = true;

        // Sections
        private const string _sections = "Sections";

        [Category(_sections)]
        [DisplayName("Validate globbing patterns")]
        [Description("Show an error if a globbing pattern isn't matching any files on disk.")]
        [DefaultValue(true)]
        public bool EnableGlobbingMatcher { get; set; } = true;

        [Category(_sections)]
        [DisplayName("Allow spaces in sections")]
        [Description("Spaces in globbing patterns are allowed, but are often the result of a typo.")]
        [DefaultValue(false)]
        public bool AllowSpacesInSections { get; set; }

        /// <summary>
        /// Checks if a property keyword should be ignored based on the configured ignored prefixes.
        /// </summary>
        public bool HasIgnoredPrefix(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return false;

            // Lazy initialize and cache the parsed prefixes
            _cachedPrefixes ??= ParsePrefixes(_ignoredPrefixes);

            foreach (string prefix in _cachedPrefixes)
            {
                if (keyword.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a property keyword should be ignored based on the provided prefix list.
        /// </summary>
        /// <param name="keyword">The keyword to check.</param>
        /// <param name="prefixList">Comma-separated list of prefixes to ignore.</param>
        /// <returns>True if the keyword starts with any of the ignored prefixes.</returns>
        public static bool HasIgnoredPrefix(string keyword, string prefixList)
        {
            if (string.IsNullOrEmpty(prefixList) || string.IsNullOrEmpty(keyword))
                return false;

            string[] prefixes = ParsePrefixes(prefixList);
            foreach (string prefix in prefixes)
            {
                if (keyword.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string[] ParsePrefixes(string prefixList)
        {
            if (string.IsNullOrEmpty(prefixList))
                return [];

            string[] parts = prefixList.Split([','], StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>(parts.Length);
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }
            return [.. result];
        }

        public override void SaveSettingsToStorage()
        {
            Telemetry.TrackOperation("ValidationOptionsSaved");
            base.SaveSettingsToStorage();
            Saved?.Invoke(this, EventArgs.Empty);
        }

        public static event EventHandler Saved;
    }
}
