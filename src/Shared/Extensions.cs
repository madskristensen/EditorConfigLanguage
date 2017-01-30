using System;

namespace EditorConfig
{
    public static class Extensions
    {
        /// <summary>Performs a OrdinalIgnoreCase comparison between two strings.</summary>
        public static bool Is(this string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
