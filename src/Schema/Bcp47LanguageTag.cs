using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    internal static class Bcp47LanguageTag
    {
        private static readonly HashSet<string> _grandfathered = new(StringComparer.OrdinalIgnoreCase)
        {
            "art-lojban", "cel-gaulish", "en-GB-oed", "i-ami", "i-bnn", "i-default",
            "i-enochian", "i-hak", "i-klingon", "i-lux", "i-mingo", "i-navajo",
            "i-pwn", "i-tao", "i-tay", "i-tsu", "no-bok", "no-nyn", "sgn-BE-FR",
            "sgn-BE-NL", "sgn-CH-DE", "zh-guoyu", "zh-hakka", "zh-min", "zh-min-nan",
            "zh-xiang"
        };

        internal static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 255)
                return false;

            if (_grandfathered.Contains(value))
                return true;

            string[] parts = value.Split('-');
            if (parts.Any(part => part.Length == 0 || part.Length > 8 || !part.All(IsAsciiLetterOrDigit)))
                return false;

            if (parts[0].Is("x"))
                return parts.Length > 1;

            if (parts[0].Length < 2 || parts[0].Length > 8 || !parts[0].All(IsAsciiLetter))
                return false;

            int index = 1;

            if (parts[0].Length <= 3)
            {
                int extlangCount = 0;
                while (index < parts.Length && extlangCount < 3 &&
                       parts[index].Length == 3 && parts[index].All(IsAsciiLetter))
                {
                    index++;
                    extlangCount++;
                }
            }

            if (index < parts.Length && parts[index].Length == 4 && parts[index].All(IsAsciiLetter))
                index++;

            if (index < parts.Length &&
                ((parts[index].Length == 2 && parts[index].All(IsAsciiLetter)) ||
                 (parts[index].Length == 3 && parts[index].All(IsAsciiDigit))))
            {
                index++;
            }

            while (index < parts.Length && IsVariant(parts[index]))
                index++;

            while (index < parts.Length && parts[index].Length == 1 && !parts[index].Is("x"))
            {
                index++;
                int extensionStart = index;

                while (index < parts.Length && parts[index].Length >= 2)
                    index++;

                if (index == extensionStart)
                    return false;
            }

            if (index < parts.Length && parts[index].Is("x"))
                return index + 1 < parts.Length;

            return index == parts.Length;
        }

        private static bool IsVariant(string part)
            => (part.Length >= 5 && part.Length <= 8) ||
               (part.Length == 4 && IsAsciiDigit(part[0]));

        private static bool IsAsciiLetterOrDigit(char value)
            => IsAsciiLetter(value) || IsAsciiDigit(value);

        private static bool IsAsciiLetter(char value)
            => value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';

        private static bool IsAsciiDigit(char value)
            => value >= '0' && value <= '9';
    }
}
