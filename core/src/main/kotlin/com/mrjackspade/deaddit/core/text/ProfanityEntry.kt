package com.mrjackspade.deaddit.core.text

/**
 * One entry in dsojevic/profanity-list format. In [match], "|" separates alternative terms and "*" means the
 * previous character may repeat. In [exceptions], "*" stands for the matched term, so "s*horpe" exempts "cunt"
 * inside "scunthorpe".
 *
 * Equality is identity, as for the C# class: [ProfanityMatcher] tells supplement entries apart from upstream ones
 * by reference.
 */
class ProfanityEntry(
    val id: String,
    val match: String,
    val severity: Int = 0,
    val tags: List<String> = emptyList(),
    val exceptions: List<String> = emptyList(),
)
