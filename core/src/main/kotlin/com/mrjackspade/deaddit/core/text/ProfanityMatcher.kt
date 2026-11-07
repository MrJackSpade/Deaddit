package com.mrjackspade.deaddit.core.text

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.int
import java.util.regex.Pattern

/** A half-open character range [start, end) of a string. */
data class TextSpan(val start: Int, val end: Int) : Comparable<TextSpan> {
    override fun compareTo(other: TextSpan): Int =
        if (start != other.start) start.compareTo(other.start) else end.compareTo(other.end)
}

/**
 * Finds swear words using the vendored dsojevic/profanity-list (resources `.../text/profanity/en.json`) as tuned by
 * [ProfanityPolicy].
 */
class ProfanityMatcher(upstream: List<ProfanityEntry>) {
    private val rules = ArrayList<Rule>()

    init {
        val all = upstream + ProfanityPolicy.supplement

        ensurePolicyIdsExist(all)

        for (entry in all.filter(::isIncluded)) {
            val partial = entry.id in ProfanityPolicy.partialIds

            val extra = ProfanityPolicy.extraExceptions[entry.id]
            val exceptions = if (extra != null) entry.exceptions + extra else entry.exceptions

            val parsedExceptions = exceptions.map(::parseException)

            for (term in entry.match.split('|')) {
                val termPattern = "(?<term>${termPattern(term)})"

                val pattern = if (partial) {
                    termPattern
                } else {
                    "(?<!${DotNetText.LETTER_CLASS})$termPattern$WHOLE_WORD_SUFFIX(?!${DotNetText.LETTER_CLASS})"
                }

                rules.add(Rule(Pattern.compile(pattern), parsedExceptions))
            }
        }
    }

    /** Returns the sorted, non-overlapping character ranges of [text] that are swearing. */
    fun findMatches(text: String): List<TextSpan> {
        // Lowercasing invariantly is char-for-char, so indexes line up with the original text.
        val lower = DotNetText.toLowerInvariant(text)

        val spans = ArrayList<TextSpan>()

        for (rule in rules) {
            val matcher = rule.pattern.matcher(lower)

            while (matcher.find()) {
                if (!rule.isExcepted(lower, matcher.start("term"), matcher.end("term"))) {
                    spans.add(TextSpan(matcher.start(), matcher.end()))
                }
            }
        }

        spans.sort()

        val merged = ArrayList<TextSpan>()

        for (span in spans) {
            if (merged.isNotEmpty() && span.start < merged.last().end) {
                val last = merged.last()
                merged[merged.lastIndex] = TextSpan(last.start, maxOf(last.end, span.end))
            } else {
                merged.add(span)
            }
        }

        return merged
    }

    private class WordException(val prefix: String, val suffix: String)

    private class Rule(val pattern: Pattern, val exceptions: List<WordException>) {
        fun isExcepted(text: String, start: Int, end: Int): Boolean = exceptions.any { e ->
            start >= e.prefix.length &&
                end + e.suffix.length <= text.length &&
                text.regionMatches(start - e.prefix.length, e.prefix, 0, e.prefix.length) &&
                text.regionMatches(end, e.suffix, 0, e.suffix.length)
        }
    }

    companion object {
        private const val RESOURCE_NAME = "/com/mrjackspade/deaddit/core/text/profanity/en.json"

        // Endings allowed after a whole-word match ("bastards", "pissed", "cocking").
        private const val WHOLE_WORD_SUFFIX = "(?:s|es|ed|d|ing)?"

        val Default: ProfanityMatcher by lazy { ProfanityMatcher(readEmbeddedList()) }

        private fun ensurePolicyIdsExist(all: List<ProfanityEntry>) {
            val known = all.mapTo(HashSet()) { it.id }

            val missing = (
                ProfanityPolicy.includedIds.asSequence() +
                    ProfanityPolicy.excludedIds +
                    ProfanityPolicy.partialIds +
                    ProfanityPolicy.extraExceptions.keys
                ).filter { it !in known }.toList()

            if (missing.isNotEmpty()) {
                throw IllegalStateException("Profanity policy references ids missing from the list: ${missing.joinToString(", ")}")
            }
        }

        private fun isIncluded(entry: ProfanityEntry): Boolean {
            if (entry.id in ProfanityPolicy.excludedIds || entry.tags.any { it in ProfanityPolicy.excludedTags }) {
                return false
            }

            return entry.id in ProfanityPolicy.includedIds ||
                entry.tags.any { it in ProfanityPolicy.includedTags } ||
                ProfanityPolicy.supplement.any { it === entry }
        }

        private fun parseException(exception: String): WordException {
            val star = exception.indexOf('*')

            if (star < 0) {
                throw IllegalStateException("Profanity exception '$exception' has no '*' placeholder")
            }

            return WordException(exception.substring(0, star), exception.substring(star + 1))
        }

        private fun readEmbeddedList(): List<ProfanityEntry> {
            val text = (ProfanityMatcher::class.java.getResourceAsStream(RESOURCE_NAME)
                ?: throw IllegalStateException("Embedded resource '$RESOURCE_NAME' not found"))
                .use { it.readBytes().toString(Charsets.UTF_8) }

            val root = Json.parseToJsonElement(text)

            if (root is JsonNull) {
                throw IllegalStateException("Embedded resource '$RESOURCE_NAME' is empty")
            }

            return (root as JsonArray).map { readEntry(it as JsonObject) }
        }

        // Matches System.Text.Json with PropertyNameCaseInsensitive: names match ignoring case, the last
        // duplicate wins, unknown properties are ignored, and id and match are required.
        private fun readEntry(entry: JsonObject): ProfanityEntry {
            fun property(name: String): JsonElement? =
                entry.entries.lastOrNull { it.key.equals(name, ignoreCase = true) }?.value

            fun string(name: String): String {
                val value = property(name) ?: throw IllegalStateException("Profanity entry is missing required property '$name': $entry")
                val primitive = value as JsonPrimitive
                check(primitive.isString) { "Profanity entry property '$name' is not a string: $entry" }
                return primitive.content
            }

            fun strings(name: String): List<String>? = property(name)?.let { value ->
                (value as JsonArray).map { item ->
                    val primitive = item as JsonPrimitive
                    check(primitive.isString) { "Profanity entry property '$name' has a non-string item: $entry" }
                    primitive.content
                }
            }

            val severity = property("severity")?.let { value ->
                val primitive = value as JsonPrimitive
                check(!primitive.isString) { "Profanity entry property 'severity' is not a number: $entry" }
                primitive.int
            }

            return ProfanityEntry(
                id = string("id"),
                match = string("match"),
                severity = severity ?: 0,
                tags = strings("tags") ?: emptyList(),
                exceptions = strings("exceptions") ?: emptyList(),
            )
        }

        private fun termPattern(term: String): String {
            val pattern = StringBuilder()

            for (c in term) {
                when (c) {
                    '*' -> pattern.append("+")
                    ' ' -> pattern.append(DotNetText.WHITESPACE_CLASS).append('+')
                    else -> pattern.append(escape(c))
                }
            }

            return pattern.toString()
        }

        // Regex.Escape: a literal for the one char. Java treats a backslash before any non-letter, non-digit
        // ASCII char as that char literally; letters, digits and non-ASCII chars are literal unescaped.
        private fun escape(c: Char): String =
            if (c.code < 0x80 && !c.isLetterOrDigit()) "\\$c" else c.toString()
    }
}
