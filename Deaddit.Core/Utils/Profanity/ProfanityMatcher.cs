using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Deaddit.Core.Utils.Profanity
{
    /// <summary>
    /// Finds swear words using the vendored dsojevic/profanity-list (Resources/Profanity/en.json) as tuned
    /// by <see cref="ProfanityPolicy"/>.
    /// </summary>
    public class ProfanityMatcher
    {
        private const string RESOURCE_NAME = "Deaddit.Core.Resources.Profanity.en.json";

        // Endings allowed after a whole-word match ("bastards", "pissed", "cocking").
        private const string WHOLE_WORD_SUFFIX = "(?:s|es|ed|d|ing)?";

        private static readonly Lazy<ProfanityMatcher> _default = new(() => new ProfanityMatcher(ReadEmbeddedList()));

        private readonly List<Rule> _rules = [];

        public ProfanityMatcher(IReadOnlyList<ProfanityEntry> upstream)
        {
            List<ProfanityEntry> all = [.. upstream, .. ProfanityPolicy.Supplement];

            EnsurePolicyIdsExist(all);

            foreach (ProfanityEntry entry in all.Where(IsIncluded))
            {
                bool partial = ProfanityPolicy.PartialIds.Contains(entry.Id);

                IEnumerable<string> exceptions = ProfanityPolicy.ExtraExceptions.TryGetValue(entry.Id, out string[]? extra)
                    ? entry.Exceptions.Concat(extra)
                    : entry.Exceptions;

                (string Prefix, string Suffix)[] parsedExceptions = [.. exceptions.Select(ParseException)];

                foreach (string term in entry.Match.Split('|'))
                {
                    string termPattern = $"(?<term>{TermPattern(term)})";

                    string pattern = partial
                        ? termPattern
                        : $@"(?<!\p{{L}}){termPattern}{WHOLE_WORD_SUFFIX}(?!\p{{L}})";

                    _rules.Add(new Rule(new Regex(pattern, RegexOptions.CultureInvariant), parsedExceptions));
                }
            }
        }

        public static ProfanityMatcher Default => _default.Value;

        /// <summary>
        /// Returns the sorted, non-overlapping character ranges of <paramref name="text"/> that are swearing.
        /// </summary>
        public List<(int Start, int End)> FindMatches(string text)
        {
            // Lowercasing invariantly is char-for-char, so indexes line up with the original text.
            string lower = text.ToLowerInvariant();

            List<(int Start, int End)> spans = [];

            foreach (Rule rule in _rules)
            {
                foreach (Match match in rule.Pattern.Matches(lower))
                {
                    Group term = match.Groups["term"];

                    if (!rule.IsExcepted(lower, term.Index, term.Index + term.Length))
                    {
                        spans.Add((match.Index, match.Index + match.Length));
                    }
                }
            }

            spans.Sort();

            List<(int Start, int End)> merged = [];

            foreach ((int start, int end) in spans)
            {
                if (merged.Count > 0 && start < merged[^1].End)
                {
                    merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, end));
                }
                else
                {
                    merged.Add((start, end));
                }
            }

            return merged;
        }

        private static void EnsurePolicyIdsExist(List<ProfanityEntry> all)
        {
            HashSet<string> known = [.. all.Select(e => e.Id)];

            string[] missing =
            [
                .. ProfanityPolicy.IncludedIds
                    .Concat(ProfanityPolicy.ExcludedIds)
                    .Concat(ProfanityPolicy.PartialIds)
                    .Concat(ProfanityPolicy.ExtraExceptions.Keys)
                    .Where(id => !known.Contains(id))
            ];

            if (missing.Length > 0)
            {
                throw new InvalidOperationException($"Profanity policy references ids missing from the list: {string.Join(", ", missing)}");
            }
        }

        private static bool IsIncluded(ProfanityEntry entry)
        {
            if (ProfanityPolicy.ExcludedIds.Contains(entry.Id) || entry.Tags.Any(ProfanityPolicy.ExcludedTags.Contains))
            {
                return false;
            }

            return ProfanityPolicy.IncludedIds.Contains(entry.Id)
                || entry.Tags.Any(ProfanityPolicy.IncludedTags.Contains)
                || ProfanityPolicy.Supplement.Contains(entry);
        }

        private static (string Prefix, string Suffix) ParseException(string exception)
        {
            int star = exception.IndexOf('*');

            if (star < 0)
            {
                throw new InvalidOperationException($"Profanity exception '{exception}' has no '*' placeholder");
            }

            return (exception[..star], exception[(star + 1)..]);
        }

        private static List<ProfanityEntry> ReadEmbeddedList()
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RESOURCE_NAME)
                ?? throw new InvalidOperationException($"Embedded resource '{RESOURCE_NAME}' not found");

            return JsonSerializer.Deserialize<List<ProfanityEntry>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException($"Embedded resource '{RESOURCE_NAME}' is empty");
        }

        private static string TermPattern(string term)
        {
            StringBuilder pattern = new();

            foreach (char c in term)
            {
                pattern.Append(c switch
                {
                    '*' => "+",
                    ' ' => @"\s+",
                    _ => Regex.Escape(c.ToString())
                });
            }

            return pattern.ToString();
        }

        private sealed record Rule(Regex Pattern, (string Prefix, string Suffix)[] Exceptions)
        {
            public bool IsExcepted(string text, int start, int end)
            {
                return Exceptions.Any(e =>
                    start >= e.Prefix.Length
                    && end + e.Suffix.Length <= text.Length
                    && text.AsSpan(start - e.Prefix.Length, e.Prefix.Length).SequenceEqual(e.Prefix)
                    && text.AsSpan(end, e.Suffix.Length).SequenceEqual(e.Suffix));
            }
        }
    }
}
