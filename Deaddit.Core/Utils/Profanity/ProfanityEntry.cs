namespace Deaddit.Core.Utils.Profanity
{
    /// <summary>
    /// One entry in dsojevic/profanity-list format. In <see cref="Match"/>, "|" separates alternative terms
    /// and "*" means the previous character may repeat. In <see cref="Exceptions"/>, "*" stands for the matched
    /// term, so "s*horpe" exempts "cunt" inside "scunthorpe".
    /// </summary>
    public class ProfanityEntry
    {
        public string[] Exceptions { get; init; } = [];

        public required string Id { get; init; }

        public required string Match { get; init; }

        public int Severity { get; init; }

        public string[] Tags { get; init; } = [];
    }
}
