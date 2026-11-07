package com.mrjackspade.deaddit.core.util

import com.mrjackspade.deaddit.core.ParityCorpus
import com.mrjackspade.deaddit.core.model.isAbsoluteUrl
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * DotNetUri against .NET 10's System.Uri. The corpora were recorded by running .NET on each input (the scratch probe
 * that wrote them called the same System.Uri and HttpResponseHeaders members the C# app did). Inputs whose .NET
 * result depends on the OS (rooted paths, DOS paths, file: URIs) were left out: Android is Unix, the recorder ran on
 * Windows.
 */
class DotNetUriTests {
    @Test
    fun isAbsoluteUrl_AcceptsWhatDotNetAccepts() {
        assertTrue(isAbsoluteUrl("https://example.com/a b"))
        assertTrue(isAbsoluteUrl("https://example.com/a|b%"))
        assertTrue(isAbsoluteUrl("https://exa_mple.com/x"))
        // On Unix a rooted path is a file URI.
        assertTrue(isAbsoluteUrl("/r/pics/comments/abc/title/"))
        assertFalse(isAbsoluteUrl("r/pics"))
        assertFalse(isAbsoluteUrl("https://example.com:99999/"))
        assertFalse(isAbsoluteUrl(""))
    }

    @Test
    fun host_IsLowercasedAndKeepsUnderscores() {
        assertEquals("exa_mple.com", DotNetUri.create("https://Exa_Mple.COM/x").host)
        assertEquals("media.giphy.com", DotNetUri.create("https://MEDIA.giphy.com/media/abc/giphy.gif").host)
    }

    @Test
    fun combine_HlsSegment() {
        val combined = DotNetUri.create(DotNetUri.create("https://v.redd.it/abc/HLSPlaylist.m3u8?a=1"), "HLS_720.m3u8")
        assertEquals("https://v.redd.it/abc/HLS_720.m3u8", combined.toString())
        assertEquals("https://v.redd.it/abc/a%20b.ts", DotNetUri.create(DotNetUri.create("https://v.redd.it/abc/x.m3u8"), "a b.ts").absoluteUri)
    }

    /** Uri.TryCreate(Absolute) with Host, AbsoluteUri, ToString and AbsolutePath; new Uri(s, RelativeOrAbsolute); Headers.Location. */
    @Test
    fun parse_MatchesDotNet() {
        val rows = ParityCorpus.rows("uri-parity.tsv")
        val failures = ArrayList<String>()

        for (row in rows) {
            val input = ParityCorpus.decode(row[0])
            val uri = DotNetUri.tryCreate(input)

            fun compare(what: String, expected: String, actual: () -> String?) {
                val outcome = ParityCorpus.outcome(actual)
                if (outcome != expected) failures.add("$what(${row[0]}): expected $expected, got $outcome")
            }

            compare("host", row[1]) { uri?.host }
            compare("absoluteUri", row[2]) { uri?.absoluteUri }
            compare("toString", row[3]) { uri?.toString() }
            compare("absolutePath", row[4]) { uri?.absolutePath }
            compare("relativeOrAbsolute", row[5]) {
                val r = DotNetUri.createRelativeOrAbsolute(input)
                (if (r.isAbsoluteUri) "A:" else "R:") + r.toString()
            }
            compare("location", row[6]) { DotNetHttpHeaders.location(input) ?: "<null>" }
        }

        assertEquals("${failures.size} of ${rows.size} rows differ:\n" + failures.take(40).joinToString("\n"), 0, failures.size)
    }

    /**
     * new Uri(baseUri, relative): its ToString and AbsoluteUri, and the URL the C# M3U8Downloader's request went out with
     * (ToString parsed again by HttpClient; AndroidMessageHandler's EncodeUrl, which is that URI's AbsoluteUri when
     * it has no user info).
     */
    @Test
    fun combine_MatchesDotNet() {
        val rows = ParityCorpus.rows("uri-combine-parity.tsv")
        val failures = ArrayList<String>()

        for (row in rows) {
            val base = DotNetUri.create(ParityCorpus.decode(row[0]))
            val relative = ParityCorpus.decode(row[1])

            fun compare(what: String, expected: String, actual: () -> String?) {
                if (expected == "<skip>") return
                val outcome = ParityCorpus.outcome(actual)
                if (outcome != expected) failures.add("$what(${row[0]} + ${row[1]}): expected $expected, got $outcome")
            }

            compare("toString", row[2]) { DotNetUri.create(base, relative).toString() }
            compare("absoluteUri", row[3]) { DotNetUri.create(base, relative).absoluteUri }
            // Only http(s) URLs are fetched; for other schemes UriBuilder writes the scheme delimiter differently.
            if (!base.absoluteUri.contains('@') && row[4].startsWith("http")) {
                compare("wire", row[4]) { DotNetUri.create(DotNetUri.create(base, relative).toString()).absoluteUri }
            }
        }

        assertEquals("${failures.size} of ${rows.size} rows differ:\n" + failures.take(40).joinToString("\n"), 0, failures.size)
    }

    /** response.Headers.ETag?.Tag for the raw ETag values AndroidMessageHandler stored. */
    @Test
    fun entityTag_MatchesDotNet() {
        val rows = ParityCorpus.rows("etag-parity.tsv")
        val failures = ArrayList<String>()

        for (row in rows) {
            val values = row.drop(1).map(ParityCorpus::decode)
            val outcome = ParityCorpus.outcome { DotNetHttpHeaders.entityTag(values) ?: "<null>" }
            if (outcome != row[0]) failures.add("entityTag(${row.drop(1)}): expected ${row[0]}, got $outcome")
        }

        assertEquals("${failures.size} of ${rows.size} rows differ:\n" + failures.joinToString("\n"), 0, failures.size)
    }

    @Test
    fun entityTag_DropsWeakPrefix() {
        assertEquals("\"abc\"", DotNetHttpHeaders.entityTag(listOf("W/\"abc\"")))
        assertEquals("\"abc\"", DotNetHttpHeaders.entityTag(listOf("\"abc\"")))
        assertEquals(null, DotNetHttpHeaders.entityTag(listOf("abc")))
        assertEquals(null, DotNetHttpHeaders.entityTag(emptyList()))
    }
}
