using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common.ExtensionMethods
{
    public static class StringExtensions
    {
        public static string AppendSuffixIfNotAlreadyPresent(
            this string str,
            string suffix,
            StringComparison stringComparisonType = StringComparison.OrdinalIgnoreCase)
        {
            suffix.ThrowIfNull();

            if (!str.EndsWith(suffix, stringComparisonType))
            {
                return str + suffix;
            }

            return str;
        }

        public static string Repeat(this string str, int count)
        {
            return string.Join(string.Empty, Enumerable.Repeat(str, count));
        }

        public static string ToTitleCase(this string str)
        {
            if (string.IsNullOrWhiteSpace(str) || str.Length == 0)
            {
                return str;
            }

            return str.Substring(0, 1).ToUpper() + str[1..];
        }

        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrWhiteSpace(str) || str.Length == 0)
            {
                return str;
            }

            return str.Substring(0, 1).ToLower() + str[1..];
        }

        public static string ToKebabCase(this string str)
        {
            if (string.IsNullOrWhiteSpace(str) || str.Length == 0)
            {
                return str;
            }

            var numPrecedingUnderscores = Regex.Match(str, @"^_+");

            return numPrecedingUnderscores + Regex.Replace(str, @"([a-z0-9])([A-Z])", "$1-$2").ToLower();
        }

        public static int GetStableHashCode(this string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];

                    if (i == str.Length - 1 || str[i + 1] == '\0')
                    {
                        break;
                    }

                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        public static string ReplaceFirst(this string str, string search, string replace)
        {
            var pos = str.IndexOf(search);

            if (pos < 0)
            {
                return str;
            }

            return $"{str.Substring(0, pos)}{replace}{str.Substring(pos + search.Length)}";
        }

        public static string TrimIfTooLong(this string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str) || maxLength <= 0)
            {
                return string.Empty;
            }

            if (str.Length <= maxLength)
            {
                return str;
            }

            // Subtract 3 for the length of the ellipsis
            return string.Concat(str.AsSpan(0, maxLength - 3), "...");
        }

        public static string RemoveAllWhitespaceCharacters(this string str)
        {
            return str
                .Replace(" ", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\t", string.Empty);
        }
    }
}
