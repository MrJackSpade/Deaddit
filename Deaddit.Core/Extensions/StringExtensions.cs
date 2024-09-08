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
            trimmed = str;

            if (str.Length < 2)
            {
                return false;
            }

            if (str[0] != start)
            {
                return false;
            }

            if (str[^1] != end)
            {
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
            trimmed = str;

            if (str.Length < start.Length + end.Length)
            {
                return false;
            }

            if (!str.StartsWith(start))
            {
                return false;
            }

            if (!str.EndsWith(end))
            {
                return false;
            }

            trimmed = str[start.Length..^end.Length];

            return true;
        }
    }
}