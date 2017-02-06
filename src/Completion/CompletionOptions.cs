using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace EditorConfig
{
    public class CompletionOptions : DialogPage
    {
        // General
        private const string _intellisense = "Intellisense";

        [Category(_intellisense)]
        [DisplayName("Auto-insert delimiters")]
        [Description("This will automatically insert \"=\" and \":\" characters on commit.")]
        [DefaultValue(true)]
        public bool AutoInsertDelimiters { get; set; } = true;

        [Category(_intellisense)]
        [DisplayName("Show undocumented properties")]
        [Description("This will show undocumented properties in Intellisense.")]
        [DefaultValue(false)]
        public bool ShowHiddenKeywords { get; set; }

        public override void SaveSettingsToStorage()
        {
            Telemetry.TrackOperation("CompletionOptionsSaved");
            base.SaveSettingsToStorage();
        }
    }
}
