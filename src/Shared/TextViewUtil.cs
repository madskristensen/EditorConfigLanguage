using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace EditorConfig
{
    public static class TextViewUtil
    {
        public static bool TryGetWpfTextView(string filePath, out IWpfTextView view)
        {
            view = null;
            IVsTextView vTextView = FindTextViewFor(filePath);

            if (vTextView is IVsUserData userData)
            {
                IWpfTextViewHost viewHost;
                Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out object holder);
                viewHost = (IWpfTextViewHost)holder;
                view = viewHost.TextView;
                return true;
            }

            return false;
        }

        private static IVsTextView FindTextViewFor(string filePath)
        {
            IVsWindowFrame frame = FindWindowFrame(filePath);
            if (frame != null)
            {
                if (GetTextViewFromFrame(frame, out IVsTextView textView))
                {
                    return textView;
                }
            }

            return null;
        }

        private static IEnumerable<IVsWindowFrame> EnumerateDocumentWindowFrames()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Package.GetGlobalService(typeof(SVsUIShell)) is IVsUIShell shell)
            {
                int hr = shell.GetDocumentWindowEnum(out IEnumWindowFrames framesEnum);

                if (hr == VSConstants.S_OK && framesEnum != null)
                {
                    IVsWindowFrame[] frames = new IVsWindowFrame[1];

                    while (framesEnum.Next(1, frames, out uint fetched) == VSConstants.S_OK && fetched == 1)
                    {
                        yield return frames[0];
                    }
                }
            }
        }

        private static IVsWindowFrame FindWindowFrame(string filePath)
        {
            foreach (IVsWindowFrame currentFrame in EnumerateDocumentWindowFrames())
            {
                if (IsFrameForFilePath(currentFrame, filePath))
                {
                    return currentFrame;
                }
            }

            return null;
        }

        private static bool GetPhysicalPathFromFrame(IVsWindowFrame frame, out string frameFilePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int hr = frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object propertyValue);

            if (hr == VSConstants.S_OK && propertyValue != null)
            {
                frameFilePath = propertyValue.ToString();
                return true;
            }

            frameFilePath = null;
            return false;
        }

        private static bool GetTextViewFromFrame(IVsWindowFrame frame, out IVsTextView textView)
        {
            textView = VsShellUtilities.GetTextView(frame);

            return textView != null;
        }

        private static bool IsFrameForFilePath(IVsWindowFrame frame, string filePath)
        {

            if (GetPhysicalPathFromFrame(frame, out string frameFilePath))
            {
                return string.Equals(filePath, frameFilePath, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
