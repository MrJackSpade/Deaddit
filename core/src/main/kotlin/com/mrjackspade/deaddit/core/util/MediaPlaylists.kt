package com.mrjackspade.deaddit.core.util

import org.w3c.dom.Element
import org.w3c.dom.NodeList
import java.io.StringReader
import javax.xml.parsers.DocumentBuilderFactory

/**
 * The URL an HTTP request for [url] goes out with in the C# app: HttpClient parses the string with System.Uri, and
 * AndroidMessageHandler sends that Uri's escaped form (EncodeUrl, which for an http(s) URI without user info is
 * Uri.AbsoluteUri). Throws [UriFormatException] where HttpClient did.
 */
fun dotNetRequestUrl(url: String): String = DotNetUri.create(url).absoluteUri

/** Reading HLS playlists as the C# M3U8Downloader did. */
object HlsPlaylist {
    /** The first variant's playlist URL and the default audio rendition's, resolved against [baseUrl]. */
    fun parseMaster(content: String, baseUrl: String): Pair<String?, String?> {
        var videoPlaylistUrl: String? = null
        var audioPlaylistUrl: String? = null

        val lines = content.lineSequence().iterator()
        while (lines.hasNext()) {
            val line = lines.next()
            if (line.startsWith("#EXT-X-MEDIA")) {
                // Parse audio track
                if (line.contains("TYPE=AUDIO") && line.contains("DEFAULT=YES")) {
                    val uri = getAttributeValue(line, "URI")
                    if (!uri.isNullOrEmpty()) {
                        audioPlaylistUrl = getAbsoluteUrl(baseUrl, uri)
                    }
                }
            } else if (line.startsWith("#EXT-X-STREAM-INF")) {
                // The next line should be the video playlist URL; the first one found is used.
                val playlistUrl = if (lines.hasNext()) lines.next() else null
                if (!playlistUrl.isNullOrEmpty()) {
                    val absoluteUrl = getAbsoluteUrl(baseUrl, playlistUrl)
                    if (videoPlaylistUrl == null) videoPlaylistUrl = absoluteUrl
                }
            }
        }

        return videoPlaylistUrl to audioPlaylistUrl
    }

    /** The segment URLs of a media playlist, resolved against [baseUrl]. */
    fun parseMedia(content: String, baseUrl: String): List<String> =
        content.lineSequence()
            .filter { !it.startsWith("#") && it.isNotBlank() }
            .map { getAbsoluteUrl(baseUrl, it.trim()) }
            .toList()

    /**
     * `new Uri(new Uri(baseUrl), relativeUrl).ToString()`: .NET's resolution (which unescapes what ToString shows
     * unescaped; the request then escapes it again, see [dotNetRequestUrl]). Throws [UriFormatException] where .NET did.
     */
    fun getAbsoluteUrl(baseUrl: String, relativeUrl: String): String =
        DotNetUri.create(DotNetUri.create(baseUrl), relativeUrl).toString()

    private fun getAttributeValue(line: String, attributeName: String): String? {
        for (part in line.split(',')) {
            val keyValue = part.split('=', limit = 2)
            if (keyValue.size == 2 && keyValue[0].trim().equals(attributeName, ignoreCase = true)) {
                var value = keyValue[1].trim()
                if (value.startsWith("\"") && value.endsWith("\"")) {
                    value = value.substring(1, value.length - 1)
                }
                return value
            }
        }
        return null
    }
}

/** What the C# FileStreamService took from a DASH manifest: the top video stream's BaseURL and the first audio one's. */
class DashManifest(val videoBaseUrl: String?, val audioBaseUrl: String?) {
    companion object {
        fun parse(manifest: String): DashManifest {
            val factory = DocumentBuilderFactory.newInstance().apply { isNamespaceAware = true }
            val doc = factory.newDocumentBuilder().parse(org.xml.sax.InputSource(StringReader(manifest)))

            val adaptationSets = doc.getElementsByTagNameNS("*", "AdaptationSet").elements()

            fun representations(contentType: String) = adaptationSets
                .filter { it.getAttribute("contentType") == contentType }
                .flatMap { it.getElementsByTagNameNS("*", "Representation").elements() }

            fun baseUrlOf(representation: Element): String? =
                representation.getElementsByTagNameNS("*", "BaseURL").elements().firstOrNull()?.textContent

            // Find video URL (highest bandwidth): OrderByDescending(int.TryParse(bandwidth, out bw) ? bw : 0), a stable sort.
            val video = representations("video")
                .sortedByDescending(::bandwidthRank)
                .map(::baseUrlOf)
                .firstOrNull()

            // Find audio URL
            val audio = representations("audio").map(::baseUrlOf).firstOrNull()

            return DashManifest(video, audio)
        }

        /** A Representation's place in the C# ranking: its bandwidth, or 0 when that isn't an Int32 (missing included). */
        fun bandwidthRank(representation: Element): Int =
            DotNetNumbers.tryParseInt(representation.getAttributeNode("bandwidth")?.value) ?: 0

        private fun NodeList.elements(): List<Element> = (0 until length).map { item(it) as Element }
    }
}
