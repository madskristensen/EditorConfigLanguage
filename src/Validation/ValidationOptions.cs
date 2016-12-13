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

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();
            Saved?.Invoke(this, EventArgs.Empty);
        }

        public static event EventHandler Saved;
    }
}
