package com.mrjackspade.deaddit.core.text

import com.mrjackspade.deaddit.core.ParityCorpus
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotEquals
import org.junit.Test

class SwearObfuscatorTests {
    @Test
    fun swapsHomoglyphsAndInsertsZeroWidthSpace() {
        assertEquals("fu\u200B\u0441k", SwearObfuscator.obfuscate("fuck"))
        assertEquals("\u0405\u041D\u200B\u0406\u0422", SwearObfuscator.obfuscate("SHIT"))
    }

    @Test
    fun isDeterministic() {
        val input = "well shit, that's a fucking mess"

        assertEquals(SwearObfuscator.obfuscate(input), SwearObfuscator.obfuscate(input))
    }

    @Test
    fun isIdempotent() {
        val once = SwearObfuscator.obfuscate("bullshit motherfucker ass")

        assertEquals(once, SwearObfuscator.obfuscate(once))
    }

    @Test
    fun leavesCleanTextAlone() {
        val input = "The class passed the scrap assessment, swank."

        assertEquals(input, SwearObfuscator.obfuscate(input))
    }

    @Test
    fun catchesSwear() {
        val swears = listOf(
            "fuck", "Fucking", "motherfucker", "un-fucking-believable", "shit", "bullshitting", "dipshit", "shitty", "cunt",
            "bitches", "son-of-a-bitch", "bastards", "asshole", "ass", "half-assed", "dumbass", "damn", "goddamnit", "dammit",
            "hell", "crap", "cock", "cocksucker", "dick", "dickhead", "pussy", "tits", "twat", "wanker", "bollocks", "piss",
            "pissed", "prick", "douchebag", "bloody", "piece of shit",
        )

        for (swear in swears) {
            assertEquals(swear, 1, ProfanityMatcher.Default.findMatches(swear).size)
        }
    }

    @Test
    fun ignoresInnocentWord() {
        val words = listOf(
            "class", "pass", "assessment", "analysis", "canal", "therapist", "scrap", "Scunthorpe", "hello", "shell", "heller",
            "cocktail", "peacock", "cocked", "Dickens", "document", "accumulate", "cumin", "Essex", "Christ", "Christy",
            "Yamashita", "Kinoshita", "Shakuntala", "shiitake", "pricked", "snatched", "world domination", "fingered", "thorny",
            "swank",
        )

        for (word in words) {
            assertEquals(word, 0, ProfanityMatcher.Default.findMatches(word).size)
        }
    }

    @Test
    fun excludesSlurs() {
        assertEquals(0, ProfanityMatcher.Default.findMatches("retard spastic raghead").size)
    }

    @Test
    fun looksIdenticalOnceInvisibleCharactersAreRemovedAndScriptsFolded() {
        val obfuscated = SwearObfuscator.obfuscate("What the Fuck, you dumbass bitch.")

        assertNotEquals("What the Fuck, you dumbass bitch.", obfuscated)
        assertEquals("What the Fuck, you dumbass bitch.", fold(obfuscated))
    }

    @Test
    fun leavesUrlsCodeAndReferencesIntact() {
        val input = "r/fuckcars /u/shitposter [link](https://example.com/shit) https://fuck.com `shit` ```\nfuck\n```"

        assertEquals(input, SwearObfuscator.obfuscate(input))
    }

    @Test
    fun obfuscatesLinkTextButNotDestination() {
        assertEquals("[\u0455\u04BB\u200B\u0456t](https://example.com/shit)", SwearObfuscator.obfuscate("[shit](https://example.com/shit)"))
    }

    /** Every row of parity/obfuscator-parity.tsv: the matched spans and the obfuscated text the C# produced. */
    @Test
    fun matchesTheCSharpOnRecordedInputs() {
        val rows = ParityCorpus.rows("obfuscator-parity.tsv")

        for (row in rows) {
            val input = ParityCorpus.decode(row[0])
            val spans = ProfanityMatcher.Default.findMatches(input).joinToString(";") { "${it.start}-${it.end}" }

            assertEquals("spans of ${row[0]}", row[1], spans)
            assertEquals("obfuscation of ${row[0]}", row[2], ParityCorpus.outcome { SwearObfuscator.obfuscate(input) })
        }
    }

    private fun fold(text: String): String {
        val reverse = mapOf(
            '\u0430' to 'a', '\u0441' to 'c', '\u0501' to 'd', '\u0435' to 'e', '\u04BB' to 'h',
            '\u0456' to 'i', '\u0458' to 'j', '\u043E' to 'o', '\u0440' to 'p', '\u051B' to 'q',
            '\u0455' to 's', '\u051D' to 'w', '\u0445' to 'x', '\u0443' to 'y', '\u0410' to 'A',
            '\u0412' to 'B', '\u0421' to 'C', '\u0415' to 'E', '\u041D' to 'H', '\u0406' to 'I',
            '\u0408' to 'J', '\u041A' to 'K', '\u041C' to 'M', '\u039D' to 'N', '\u041E' to 'O',
            '\u0420' to 'P', '\u0405' to 'S', '\u0422' to 'T', '\u0425' to 'X', '\u04AE' to 'Y',
            '\u0396' to 'Z',
        )

        return String(text.filter { it != '\u200B' }.map { reverse[it] ?: it }.toCharArray())
    }
}
