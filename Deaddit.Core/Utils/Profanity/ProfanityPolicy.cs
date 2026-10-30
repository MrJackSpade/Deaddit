namespace Deaddit.Core.Utils.Profanity
{
    /// <summary>
    /// Local decisions layered over the vendored upstream list: which entries count as swearing, which may
    /// match inside longer words, and the common swears upstream is missing.
    /// </summary>
    internal static class ProfanityPolicy
    {
        // Upstream tags taken wholesale.
        public static readonly HashSet<string> IncludedTags = ["general", "religious"];

        // Never included, whatever else matches.
        public static readonly HashSet<string> ExcludedTags = ["racial", "lgbtq"];

        // Slurs filed under included tags, and religious names that are mostly used non-profanely.
        public static readonly HashSet<string> ExcludedIds = ["raghead", "retard", "spastic", "christ", "jesus", "jesus-christ"];

        // The vulgar end of the "sexual" tag. The rest of that tag is clinical terms, kink vocabulary and
        // site names, and includes everyday words like "snatch", "domination" and "fingering".
        public static readonly HashSet<string> IncludedIds =
        [
            "ass", "arse", "bollocks", "boob", "cock", "dick", "tits", "titty", "wank", "jizz", "cum", "cumming",
            "cumshot", "spunk", "schlong", "splooge", "clunge", "punani", "queef", "blowjob", "hand-job", "rimjob",
            "jerk-off", "jack-off", "circlejerk", "horny", "pissing",
        ];

        // May match anywhere inside a word ("motherfucker", "dipshit"). Everything else only matches a whole
        // word plus a plural/tense ending: upstream's partial-match exceptions are far from complete, and
        // "ass" alone would otherwise hit class, pass, mass and thousands more.
        public static readonly HashSet<string> PartialIds = ["fuck", "shit", "cunt", "bitch", "bastard", "whore", "damn", "arsehole"];

        // Exceptions upstream misses, in the same format ("*" is the matched term).
        public static readonly Dictionary<string, string[]> ExtraExceptions = new()
        {
            ["shit"] = ["a*a", "i*a", "o*a", "u*a"], // Yamashita, Kinoshita, Morishita
            ["cunt"] = ["*a"], // Kunta, Shakuntala
        };

        // Common swears upstream lacks.
        public static readonly ProfanityEntry[] Supplement =
        [
            new() { Id = "piss", Match = "piss|pissy" },
            new() { Id = "prick", Match = "prick", Exceptions = ["*ed", "*ing"] },
            new() { Id = "dickhead", Match = "dickhead|dickwad|dickface" },
            new() { Id = "cocksucker", Match = "cocksucker|cocksucking" },
            new() { Id = "ass-compound", Match = "dumbass|jackass|smartass|badass|fatass|lardass|hardass|kickass|asshat|asswipe|asskisser" },
            new() { Id = "douche", Match = "douche|douchebag|douchey" },
            new() { Id = "dammit", Match = "dammit|goddammit" },
        ];
    }
}
