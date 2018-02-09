using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Runtime.InteropServices;

namespace EditorConfig
{
    [Guid(LanguageGuid)]
    public class EditorConfigLanguage : LanguageService
    {
        public const string LanguageGuid = "f99a05b5-311b-4772-92fc-6441a78ca26f";
        private LanguagePreferences preferences = null;

        public EditorConfigLanguage(object site)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            SetSite(site);
        }

        public override Source CreateSource(IVsTextLines buffer)
        {
            return new EditorConfigSource(this, buffer, new EditorConfigColorizer(this, buffer, null));
        }

        public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView forView)
        {
            if (Preferences.ShowNavigationBar)
                return new DropDownBars(this, forView);

            return null;
        }

        public override LanguagePreferences GetLanguagePreferences()
        {
            if (preferences == null)
            {
                preferences = new LanguagePreferences(Site, typeof(EditorConfigLanguage).GUID, Name);

                if (preferences != null)
                {
                    preferences.Init();

                    preferences.EnableCodeSense = true;
                    preferences.EnableMatchBraces = true;
                    preferences.EnableMatchBracesAtCaret = true;
                    preferences.EnableShowMatchingBrace = true;
                    preferences.EnableCommenting = true;
                    preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
                    preferences.LineNumbers = true;
                    preferences.MaxErrorMessages = 100;
                    preferences.AutoOutlining = true;
                    preferences.MaxRegionTime = 2000;
                    preferences.ShowNavigationBar = true;
                    preferences.InsertTabs = false;
                    preferences.IndentSize = 2;
                    preferences.ShowNavigationBar = true;
                    preferences.EnableAsyncCompletion = true;

                    preferences.WordWrap = false;
                    preferences.WordWrapGlyphs = true;

                    preferences.AutoListMembers = true;
                    preferences.EnableQuickInfo = true;
                    preferences.ParameterInformation = true;
                    preferences.HideAdvancedMembers = false;
                }
            }

            return preferences;
        }

        public override IScanner GetScanner(IVsTextLines buffer)
        {
            return null;
        }

        public override AuthoringScope ParseSource(ParseRequest req)
        {
            return null;
        }

        public override string GetFormatFilterList()
        {
            return $"EditorConfig File (*{Constants.FileName})|*{Constants.FileName}";
        }

        public override string Name => Constants.LanguageName;

        public sealed override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool dispose)
        {
            try
            {
                if (preferences != null)
                {
                    preferences.Dispose();
                    preferences = null;
                }
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
