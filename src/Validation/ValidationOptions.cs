using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;

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
        [Description("Spces in globbing patterns are allowed, but are often the result of a type.")]
        [DefaultValue(false)]
        public bool AllowSpacesInSections { get; set; }

        public override void SaveSettingsToStorage()
        {
            Telemetry.TrackOperation("ValidationOptionsSaved");
            base.SaveSettingsToStorage();
            Saved?.Invoke(this, EventArgs.Empty);
        }

        public static event EventHandler Saved;
    }
}
