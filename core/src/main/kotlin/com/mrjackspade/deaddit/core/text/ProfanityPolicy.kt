package com.mrjackspade.deaddit.core.text

/**
 * Local decisions layered over the vendored upstream list: which entries count as swearing, which may match inside
 * longer words, and the common swears upstream is missing.
 */
internal object ProfanityPolicy {
    // Upstream tags taken wholesale.
    val includedTags: Set<String> = linkedSetOf("general", "religious")

    // Never included, whatever else matches.
    val excludedTags: Set<String> = linkedSetOf("racial", "lgbtq")

    // Slurs filed under included tags, and religious names that are mostly used non-profanely.
    val excludedIds: Set<String> = linkedSetOf("raghead", "retard", "spastic", "christ", "jesus", "jesus-christ")

    // The vulgar end of the "sexual" tag. The rest of that tag is clinical terms, kink vocabulary and site names,
    // and includes everyday words like "snatch", "domination" and "fingering".
    val includedIds: Set<String> = linkedSetOf(
        "ass", "arse", "bollocks", "boob", "cock", "dick", "tits", "titty", "wank", "jizz", "cum", "cumming",
        "cumshot", "spunk", "schlong", "splooge", "clunge", "punani", "queef", "blowjob", "hand-job", "rimjob",
        "jerk-off", "jack-off", "circlejerk", "horny", "pissing",
    )

    // May match anywhere inside a word ("motherfucker", "dipshit"). Everything else only matches a whole word plus
    // a plural/tense ending: upstream's partial-match exceptions are far from complete, and "ass" alone would
    // otherwise hit class, pass, mass and thousands more.
    val partialIds: Set<String> = linkedSetOf("fuck", "shit", "cunt", "bitch", "bastard", "whore", "damn", "arsehole")

    // Exceptions upstream misses, in the same format ("*" is the matched term).
    val extraExceptions: Map<String, List<String>> = linkedMapOf(
        "shit" to listOf("a*a", "i*a", "o*a", "u*a"), // Yamashita, Kinoshita, Morishita
        "cunt" to listOf("*a"), // Kunta, Shakuntala
    )

    // Common swears upstream lacks.
    val supplement: List<ProfanityEntry> = listOf(
        ProfanityEntry(id = "piss", match = "piss|pissy"),
        ProfanityEntry(id = "prick", match = "prick", exceptions = listOf("*ed", "*ing")),
        ProfanityEntry(id = "dickhead", match = "dickhead|dickwad|dickface"),
        ProfanityEntry(id = "cocksucker", match = "cocksucker|cocksucking"),
        ProfanityEntry(id = "ass-compound", match = "dumbass|jackass|smartass|badass|fatass|lardass|hardass|kickass|asshat|asswipe|asskisser"),
        ProfanityEntry(id = "douche", match = "douche|douchebag|douchey"),
        ProfanityEntry(id = "dammit", match = "dammit|goddammit"),
    )
}
