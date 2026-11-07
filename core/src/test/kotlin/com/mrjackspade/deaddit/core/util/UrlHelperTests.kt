package com.mrjackspade.deaddit.core.util

import com.mrjackspade.deaddit.core.ParityCorpus
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class UrlHelperTests {
    @Test
    fun getDisplayHost_ExternalLink_ReturnsHost() {
        assertEquals("example.com", UrlHelper.getDisplayHost("https://example.com/article"))
    }

    @Test
    fun getDisplayHost_NothingToShow_ReturnsNull() {
        val urls = listOf(
            null,
            "",
            "/r/pics/comments/abc123/title/",
            "file:///r/pics/comments/abc123/title/",
            "https://i.redd.it/abc.jpg",
            "https://v.redd.it/abc",
            "https://www.reddit.com/gallery/abc123",
        )

        for (url in urls) {
            assertNull(url, UrlHelper.getDisplayHost(url))
        }
    }

    @Test
    fun isImageAndVideoFile() {
        assertTrue(UrlHelper.isImageFile("https://i.redd.it/abc.JPG?width=640"))
        assertFalse(UrlHelper.isImageFile("https://i.redd.it/abc.mp4"))
        assertTrue(UrlHelper.isVideoFile("clip.GIFV"))
        assertFalse(UrlHelper.isVideoFile("clip"))
    }

    /**
     * Every row of parity/urlhelper-parity.tsv: GetDisplayHost, GetMimeTypeFromUri, GetExtension and GetFileName as
     * the C# returned them, for hand-picked URLs plus thousands of generated ones (odd schemes, IPv4/IPv6 forms,
     * IDN hosts, escapes, dot segments, backslashes, whitespace). Inputs whose .NET result depends on the OS were not
     * recorded (implicit file paths, file: URIs, and GetExtension/GetFileName of strings with backslashes).
     */
    @Test
    fun matchesTheCSharpOnRecordedInputs() {
        val rows = ParityCorpus.rows("urlhelper-parity.tsv")
        val failures = ArrayList<String>()

        for (row in rows) {
            val input = ParityCorpus.decode(row[0])

            fun compare(what: String, expected: String, actual: () -> String?) {
                if (expected == "<skip>") return
                val outcome = ParityCorpus.outcome(actual)
                if (outcome != expected) failures.add("$what(${row[0]}): expected $expected, got $outcome")
            }

            compare("getDisplayHost", row[1]) { UrlHelper.getDisplayHost(input) }
            compare("getMimeTypeFromUri", row[2]) { UrlHelper.getMimeTypeFromUri(input) }
            compare("getExtension", row[3]) { UrlHelper.getExtension(input) }
            compare("getFileName", row[4]) { UrlHelper.getFileName(input) }
        }

        assertEquals("${failures.size} of ${rows.size} rows differ:\n" + failures.take(40).joinToString("\n"), 0, failures.size)
    }
}
