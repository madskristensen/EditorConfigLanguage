using System;
using System.Globalization;

using Microsoft.VisualStudio.Shell;

namespace EditorConfig
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ProvideBraceCompletionAttribute(string languageName) : RegistrationAttribute
    {
        public override void Register(RegistrationContext context)
        {
            string keyName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}\\{2}", "Languages", "Language Services", languageName);
            using (Key langKey = context.CreateKey(keyName))
            {
                langKey.SetValue("ShowBraceCompletion", 1);
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            string keyName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}\\{2}", "Languages", "Language Services", languageName);
            context.RemoveKey(keyName);
        }
    }
}
