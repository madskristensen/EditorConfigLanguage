using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;

namespace EditorConfig
{
    public class FormatterOptions : DialogPage
    {
        // Formatting
        private const string _properties = "Properties";

        [Category(_properties)]
        [DisplayName("Align values mode")]
        [Description("Determins if the = character should be aligned by section, document or not at all")]
        [DefaultValue(FormattingType.Section)]
        [TypeConverter(typeof(EnumConverter))]
        public FormattingType FormattingType { get; set; } = FormattingType.Section;

        [Category(_properties)]
        [DisplayName("Spaces before =")]
        [Description("Space characters to add in front of the = character")]
        [DefaultValue(1)]
        public int SpacesBeforeEquals { get; set; } = 1;

        [Category(_properties)]
        [DisplayName("Spaces after =")]
        [Description("Space characters to add after the = character")]
        [DefaultValue(1)]
        public int SpacesAfterEquals { get; set; } = 1;

        // Severity
        private const string _severity = "Severity";

        [Category(_severity)]
        [DisplayName("Spaces before :")]
        [Description("[C# only] Space characters to add in front of the : character used to specify Severity")]
        [DefaultValue(1)]
        public int SpacesBeforeColon { get; set; } = 1;

        [Category(_severity)]
        [DisplayName("Spaces after :")]
        [Description("[C# only] Space characters to add after the : character used to specify Severity")]
        [DefaultValue(1)]
        public int SpacesAfterColon { get; set; } = 1;

        public override void SaveSettingsToStorage()
        {
            Telemetry.TrackOperation("FormattingOptionsSaved");
            base.SaveSettingsToStorage();
            Saved?.Invoke(this, EventArgs.Empty);
        }

        public static event EventHandler Saved;
    }
}
