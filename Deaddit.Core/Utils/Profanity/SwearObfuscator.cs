using System.Text;
using System.Text.RegularExpressions;

namespace Deaddit.Core.Utils.Profanity
{
    /// <summary>
    /// Deterministically rewrites swear words in outgoing markdown so they render identically but no
    /// longer match plain-text filters: every Latin letter that has a visually identical counterpart in
    /// another script (Cyrillic/Greek) is swapped for it, and a zero width space is placed mid-word.
    /// What counts as a swear is decided by <see cref="ProfanityMatcher"/>.
    /// Code, URLs, link destinations and r/ u/ references are left untouched so they keep working.
    /// </summary>
    public static partial class SwearObfuscator
    {
        private const char ZERO_WIDTH_SPACE = '\u200B';

        private static readonly Dictionary<char, char> _homoglyphs = new()
        {
            ['a'] = '\u0430', // а
            ['c'] = '\u0441', // с
            ['d'] = '\u0501', // ԁ
            ['e'] = '\u0435', // е
            ['h'] = '\u04BB', // һ
            ['i'] = '\u0456', // і
            ['j'] = '\u0458', // ј
            ['o'] = '\u043E', // о
            ['p'] = '\u0440', // р
            ['q'] = '\u051B', // ԛ
            ['s'] = '\u0455', // ѕ
            ['w'] = '\u051D', // ԝ
            ['x'] = '\u0445', // х
            ['y'] = '\u0443', // у
            ['A'] = '\u0410', // А
            ['B'] = '\u0412', // В
            ['C'] = '\u0421', // С
            ['E'] = '\u0415', // Е
            ['H'] = '\u041D', // Н
            ['I'] = '\u0406', // І
            ['J'] = '\u0408', // Ј
            ['K'] = '\u041A', // К
            ['M'] = '\u041C', // М
            ['N'] = '\u039D', // Ν (Greek)
            ['O'] = '\u041E', // О
            ['P'] = '\u0420', // Р
            ['S'] = '\u0405', // Ѕ
            ['T'] = '\u0422', // Т
            ['X'] = '\u0425', // Х
            ['Y'] = '\u04AE', // Ү
            ['Z'] = '\u0396', // Ζ (Greek)
        };

        public static string Obfuscate(string text)
        {
            StringBuilder result = new(text.Length);
            int position = 0;

            foreach (Match protectedSpan in ProtectedSpanRegex().Matches(text))
            {
                result.Append(ObfuscateSwears(text[position..protectedSpan.Index]));
                result.Append(protectedSpan.Value);
                position = protectedSpan.Index + protectedSpan.Length;
            }

            result.Append(ObfuscateSwears(text[position..]));

            return result.ToString();
        }

        private static string ObfuscateSpan(string span)
        {
            StringBuilder result = new(span.Length + 1);

            for (int i = 0; i < span.Length; i++)
            {
                if (i == span.Length / 2)
                {
                    result.Append(ZERO_WIDTH_SPACE);
                }

                result.Append(_homoglyphs.TryGetValue(span[i], out char swapped) ? swapped : span[i]);
            }

            return result.ToString();
        }

        private static string ObfuscateSwears(string text)
        {
            StringBuilder result = new(text.Length);
            int position = 0;

            foreach ((int start, int end) in ProfanityMatcher.Default.FindMatches(text))
            {
                result.Append(text, position, start - position);
                result.Append(ObfuscateSpan(text[start..end]));
                position = end;
            }

            result.Append(text, position, text.Length - position);

            return result.ToString();
        }

        // Fenced code, inline code, markdown link destinations, bare URLs, r/ and u/ references.
        [GeneratedRegex(@"```[\s\S]*?```|`[^`\n]*`|\]\([^)]*\)|(?:https?://|www\.)\S+|(?<![\w/])/?[ru]/\w+", RegexOptions.IgnoreCase)]
        private static partial Regex ProtectedSpanRegex();
    }
}
