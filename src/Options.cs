using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace EditorConfig
{
    public class Options : DialogPage
    {
        [Category("Formatting")]
        [DisplayName("Align values")]
        [Description("Determins if the = character should be aligned by section, document or not at all")]
        [DefaultValue(FormattingType.Section)]
        [TypeConverter(typeof(EnumConverter))]
        public FormattingType FormattingType { get; set; }
    }
}
