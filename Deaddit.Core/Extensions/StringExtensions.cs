using System.Diagnostics.CodeAnalysis;

namespace Deaddit.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool TryTrim(this string str, char start, [NotNullWhen(true)] out string? trimmed)
        {
            return str.TryTrim(start, start, out trimmed);
        }

        public static bool TryTrim(this string str, char start, char end, [NotNullWhen(true)] out string? trimmed)
        {
            if (str.Length < 2)
            {
                trimmed = null;
                return false;
            }

            if (str[0] != start)
            {
                trimmed = null;
                return false;
            }

            if (str[^1] != end)
            {
                trimmed = null;
                return false;
            }

            trimmed = str[1..^1];

            return true;
        }

        public static bool TryTrim(this string str, string start, [NotNullWhen(true)] out string? trimmed)
        {
            return str.TryTrim(start, start, out trimmed);
        }

        public static bool TryTrim(this string str, string start, string end, [NotNullWhen(true)] out string? trimmed)
        {
            if (str.Length < start.Length + end.Length)
            {
                trimmed = null;
                return false;
            }

            if (!str.StartsWith(start))
            {
                trimmed = null;
                return false;
            }

            if (!str.EndsWith(end))
            {
                trimmed = null;
                return false;
            }

            trimmed = str[start.Length..^end.Length];

            return true;
        }
    }
}