package com.mrjackspade.deaddit.core.util

import com.mrjackspade.deaddit.core.ParityCorpus
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

/**
 * The smaller .NET behaviors the app reproduces, against corpora recorded from .NET 10 (int.TryParse,
 * char.IsWhiteSpace, Dictionary enumeration order), and the media playlist parsing that builds on them.
 */
class DotNetPortsTests {
    @Test
    fun tryParseInt_MatchesDotNet() {
        val failures = ArrayList<String>()
        for (row in ParityCorpus.rows("int-parity.tsv")) {
            val actual = DotNetNumbers.tryParseInt(ParityCorpus.decode(row[0]))?.toString() ?: "<false>"
            if (actual != row[1]) failures.add("tryParseInt(${row[0]}): expected ${row[1]}, got $actual")
        }
        assertEquals(failures.joinToString("\n"), 0, failures.size)
    }

    @Test
    fun isWhiteSpace_MatchesDotNet() {
        val expected = ParityCorpus.rows("whitespace-parity.tsv").single()[0].split(' ').map { it.toInt(16) }
        val actual = (0..0xFFFF).filter { DotNetStrings.isWhiteSpace(it.toChar()) }
        assertEquals(expected, actual)
    }

    @Test
    fun dictionaryOrder_MatchesDotNet() {
        for (row in ParityCorpus.rows("dictionary-parity.tsv")) {
            val dictionary = DotNetDictionary.ordinalIgnoreCase<String>()
            for (op in row[0].split(' ')) {
                when {
                    op == "!" -> dictionary.clear()
                    op[0] == '-' -> dictionary.remove(op.substring(1))
                    else -> op.substring(1).split('=').let { (key, value) -> dictionary[key] = value }
                }
            }
            assertEquals(row[0], row[1], dictionary.toMap().entries.joinToString(" ") { "${it.key}=${it.value}" })
        }
    }

    @Test
    fun dash_RanksRepresentationsWithoutABandwidthAsZero() {
        val manifest = """
            <MPD xmlns="urn:mpeg:dash:schema:mpd:2011">
              <Period>
                <AdaptationSet contentType="video">
                  <Representation id="a"><BaseURL>no-bandwidth.mp4</BaseURL></Representation>
                  <Representation id="b" bandwidth="abc"><BaseURL>bad-bandwidth.mp4</BaseURL></Representation>
                  <Representation id="c" bandwidth=" 1200 "><BaseURL>1200.mp4</BaseURL></Representation>
                  <Representation id="d" bandwidth="9999999999"><BaseURL>overflow.mp4</BaseURL></Representation>
                  <Representation id="e" bandwidth="800"><BaseURL>800.mp4</BaseURL></Representation>
                </AdaptationSet>
                <AdaptationSet contentType="audio">
                  <Representation bandwidth="128000"><BaseURL>audio.mp4</BaseURL></Representation>
                </AdaptationSet>
              </Period>
            </MPD>
        """.trimIndent()

        val parsed = DashManifest.parse(manifest)
        assertEquals("1200.mp4", parsed.videoBaseUrl)
        assertEquals("audio.mp4", parsed.audioBaseUrl)
    }

    @Test
    fun dash_UnparseableBandwidthsKeepTheirOrder() {
        // OrderByDescending is stable: with every key 0, the first Representation stays first.
        val manifest = """
            <MPD><Period><AdaptationSet contentType="video">
              <Representation><BaseURL>first.mp4</BaseURL></Representation>
              <Representation bandwidth="x"><BaseURL>second.mp4</BaseURL></Representation>
              <Representation bandwidth="-5"><BaseURL>negative.mp4</BaseURL></Representation>
            </AdaptationSet></Period></MPD>
        """.trimIndent()

        val parsed = DashManifest.parse(manifest)
        assertEquals("first.mp4", parsed.videoBaseUrl)
        assertNull(parsed.audioBaseUrl)
    }

    @Test
    fun hls_ResolvesAsDotNetDoes() {
        val master = """
            #EXTM3U
            #EXT-X-MEDIA:TYPE=AUDIO,GROUP-ID="a",DEFAULT=YES,URI="HLS_AUDIO_160_K.m3u8"
            #EXT-X-STREAM-INF:BANDWIDTH=1,AUDIO="a"
            HLS 720.m3u8
            #EXT-X-STREAM-INF:BANDWIDTH=2
            HLS_1080.m3u8
        """.trimIndent()

        val (video, audio) = HlsPlaylist.parseMaster(master, "https://v.redd.it/abc/HLSPlaylist.m3u8?a=1&f=sd%2Chd")
        // java.net.URI refused the space; .NET keeps it in ToString and escapes it on the wire.
        assertEquals("https://v.redd.it/abc/HLS 720.m3u8", video)
        assertEquals("https://v.redd.it/abc/HLS%20720.m3u8", dotNetRequestUrl(video!!))
        assertEquals("https://v.redd.it/abc/HLS_AUDIO_160_K.m3u8", audio)

        val segments = HlsPlaylist.parseMedia("#EXTM3U\n#EXTINF:4,\nseg|0.ts\n\n  ../seg1.ts  \n", "https://v.redd.it/abc/HLS_720.m3u8")
        assertEquals(listOf("https://v.redd.it/abc/seg|0.ts", "https://v.redd.it/seg1.ts"), segments)
        assertEquals("https://v.redd.it/abc/seg%7C0.ts", dotNetRequestUrl(segments[0]))
    }
}
