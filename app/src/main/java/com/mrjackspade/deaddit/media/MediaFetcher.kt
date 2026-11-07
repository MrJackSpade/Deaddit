package com.mrjackspade.deaddit.media

import android.media.MediaCodec
import android.media.MediaExtractor
import android.media.MediaFormat
import android.media.MediaMuxer
import android.os.Build
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import org.w3c.dom.Element
import java.io.File
import java.io.StringReader
import java.net.URI
import java.nio.ByteBuffer
import java.util.UUID
import javax.xml.parsers.DocumentBuilderFactory

/**
 * Fetches a media URL into a file (the C# FileStreamService): HLS playlists as their video
 * segments, DASH playlists as the top-quality video muxed with its audio, anything else as is.
 */
class MediaFetcher(private val http: OkHttpClient, private val tempDir: File) {

    suspend fun fetch(url: String, target: File) = withContext(Dispatchers.IO) {
        var name = url.substringAfterLast('/')
        if (name.contains('?')) name = name.substring(0, name.indexOf('?'))
        val ext = name.substringAfterLast('.', "")

        target.parentFile?.mkdirs()

        when {
            ext.equals("m3u8", ignoreCase = true) -> {
                val media = M3U8Downloader(http).download(url, tempDir)
                try {
                    // As in the C#: only the video segments are kept; the audio is downloaded and dropped.
                    media.video.copyTo(target, overwrite = true)
                } finally {
                    media.video.delete()
                    media.audio?.delete()
                }
            }

            ext.equals("mpd", ignoreCase = true) -> fetchMuxedDash(url, target)

            else -> download(url, target)
        }
    }

    /** GETs [url] into [target]; a non-success status is an error. */
    fun download(url: String, target: File) {
        http.newCall(Request.Builder().url(url).build()).execute().use { response ->
            if (!response.isSuccessful) throw IllegalStateException("HTTP ${response.code} downloading $url")
            target.outputStream().use { response.body!!.byteStream().copyTo(it) }
        }
    }

    private fun getString(url: String): String =
        http.newCall(Request.Builder().url(url).build()).execute().use { response ->
            if (!response.isSuccessful) throw IllegalStateException("HTTP ${response.code} downloading $url")
            response.body!!.string()
        }

    private fun fetchMuxedDash(dashUrl: String, target: File) {
        val manifest = getString(dashUrl)

        val factory = DocumentBuilderFactory.newInstance().apply { isNamespaceAware = true }
        val doc = factory.newDocumentBuilder().parse(org.xml.sax.InputSource(StringReader(manifest)))

        val baseUrl = dashUrl.substring(0, dashUrl.lastIndexOf('/') + 1)

        val adaptationSets = doc.getElementsByTagNameNS("*", "AdaptationSet").elements()

        fun representations(contentType: String) = adaptationSets
            .filter { it.getAttribute("contentType") == contentType }
            .flatMap { it.getElementsByTagNameNS("*", "Representation").elements() }

        fun baseUrlOf(representation: Element): String? =
            representation.getElementsByTagNameNS("*", "BaseURL").elements().firstOrNull()?.textContent

        // Find video URL (highest bandwidth). Every DASH Representation carries a bandwidth.
        val videoBaseUrl = representations("video")
            .sortedByDescending { it.getAttribute("bandwidth").toInt() }
            .map(::baseUrlOf)
            .firstOrNull()
            ?: throw IllegalStateException("No video stream found in DASH manifest")

        // Find audio URL
        val audioBaseUrl = representations("audio").map(::baseUrlOf).firstOrNull()

        if (audioBaseUrl == null) {
            download(baseUrl + videoBaseUrl, target)
            return
        }

        val id = UUID.randomUUID().toString()
        val video = File(tempDir, "${id}_video.mp4")
        val audio = File(tempDir, "${id}_audio.mp4")
        tempDir.mkdirs()

        try {
            download(baseUrl + videoBaseUrl, video)
            download(baseUrl + audioBaseUrl, audio)
            mux(video, audio, target)
        } finally {
            video.delete()
            audio.delete()
        }
    }

    private fun org.w3c.dom.NodeList.elements(): List<Element> = (0 until length).map { item(it) as Element }

    companion object {
        /** Muxes the first track of [video] and of [audio] into an MP4 at [output], interleaved by time. */
        fun mux(video: File, audio: File, output: File) {
            val muxer = MediaMuxer(output.path, MediaMuxer.OutputFormat.MUXER_OUTPUT_MPEG_4)
            val videoExtractor = MediaExtractor().apply { setDataSource(video.path) }
            val audioExtractor = MediaExtractor().apply { setDataSource(audio.path) }

            try {
                videoExtractor.selectTrack(0)
                audioExtractor.selectTrack(0)

                val videoTrack = muxer.addTrack(videoExtractor.getTrackFormat(0))
                val audioTrack = muxer.addTrack(audioExtractor.getTrackFormat(0))
                muxer.start()

                val sources = listOf(
                    Source(videoExtractor, videoTrack, video.length()),
                    Source(audioExtractor, audioTrack, audio.length()),
                )
                val info = MediaCodec.BufferInfo()

                // AAC usually starts a frame before zero (Reddit's at -21333us, the encoder's
                // priming), and the muxer takes no negative times: shift both streams so the
                // earliest sample is at zero, keeping them in sync.
                val shift = -minOf(0L, sources.minOf { it.extractor.sampleTime })

                while (true) {
                    // The source whose next sample comes first, among those that haven't ended.
                    val next = sources.filter { !it.ended }.minByOrNull { it.extractor.sampleTime } ?: break
                    val buffer = next.bufferFor()

                    info.offset = 0
                    info.size = next.extractor.readSampleData(buffer, 0)
                    if (info.size < 0) {
                        next.ended = true
                        continue
                    }
                    info.presentationTimeUs = next.extractor.sampleTime + shift
                    info.flags = toCodecFlags(next.extractor.sampleFlags)

                    muxer.writeSampleData(next.track, buffer, info)
                    if (!next.extractor.advance()) next.ended = true
                }

                muxer.stop()
            } finally {
                muxer.release()
                videoExtractor.release()
                audioExtractor.release()
            }
        }

        private fun toCodecFlags(sampleFlags: Int): Int {
            var flags = 0
            if (sampleFlags and MediaExtractor.SAMPLE_FLAG_SYNC != 0) flags = flags or MediaCodec.BUFFER_FLAG_KEY_FRAME
            return flags
        }

        private class Source(val extractor: MediaExtractor, val track: Int, private val fileLength: Long) {
            private var buffer: ByteBuffer? = null

            /** Past the last sample. Its sampleTime is then -1, but a real sample's can be negative too. */
            var ended = false

            /** A buffer big enough for the next sample. */
            fun bufferFor(): ByteBuffer {
                val needed = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                    extractor.sampleSize.toInt()
                } else {
                    // No per-sample size before Android 9: the track's own bound, or the whole file, which bounds any sample.
                    val format = extractor.getTrackFormat(0)
                    if (format.containsKey(MediaFormat.KEY_MAX_INPUT_SIZE)) format.getInteger(MediaFormat.KEY_MAX_INPUT_SIZE) else fileLength.toInt()
                }
                val current = buffer
                if (current != null && current.capacity() >= needed) {
                    current.clear()
                    return current
                }
                return ByteBuffer.allocateDirect(needed).also { buffer = it }
            }
        }
    }
}

/** Downloads an HLS stream's segments (the C# M3U8Downloader). */
class M3U8Downloader(private val http: OkHttpClient) {
    /** The concatenated video segments, and the default audio's if the playlist names one. */
    class MediaData(val video: File, val audio: File?)

    fun download(m3u8Url: String, tempDir: File): MediaData {
        // Step 1: Download the master playlist content
        val masterPlaylistContent = try {
            getString(m3u8Url)
        } catch (e: Exception) {
            throw Exception("Error downloading master playlist: ${e.message}", e)
        }

        // Step 2: Parse the master playlist to find video and audio playlists
        val (videoPlaylistUrl, audioPlaylistUrl) = parseMasterPlaylist(masterPlaylistContent, m3u8Url)

        if (videoPlaylistUrl.isNullOrEmpty()) {
            throw Exception("No video playlist found in the master playlist.")
        }

        // Step 3: Download and parse the video playlist
        val videoSegmentUrls = getSegmentUrls(videoPlaylistUrl)

        // Step 4: Download and parse the audio playlist (if available)
        val audioSegmentUrls = if (!audioPlaylistUrl.isNullOrEmpty()) getSegmentUrls(audioPlaylistUrl) else null

        // Step 5: Download the video and audio segments
        tempDir.mkdirs()
        val id = UUID.randomUUID().toString()
        val video = File(tempDir, "${id}_video.ts")
        downloadSegments(videoSegmentUrls, video)

        val audio = audioSegmentUrls?.let { urls -> File(tempDir, "${id}_audio").also { downloadSegments(urls, it) } }

        return MediaData(video, audio)
    }

    private fun getString(url: String): String =
        http.newCall(Request.Builder().url(url).build()).execute().use { response ->
            if (!response.isSuccessful) throw IllegalStateException("HTTP ${response.code} downloading $url")
            response.body!!.string()
        }

    private fun downloadSegments(segmentUrls: List<String>, target: File) {
        target.outputStream().use { output ->
            for ((index, segmentUrl) in segmentUrls.withIndex()) {
                try {
                    http.newCall(Request.Builder().url(segmentUrl).build()).execute().use { response ->
                        if (!response.isSuccessful) throw IllegalStateException("HTTP ${response.code} downloading $segmentUrl")
                        response.body!!.byteStream().copyTo(output)
                    }
                } catch (e: Exception) {
                    throw Exception("Error downloading segment ${index + 1}: ${e.message}", e)
                }
            }
        }
    }

    private fun getSegmentUrls(playlistUrl: String): List<String> {
        val playlistContent = try {
            getString(playlistUrl)
        } catch (e: Exception) {
            throw Exception("Error downloading playlist: ${e.message}", e)
        }

        return parseMediaPlaylist(playlistContent, playlistUrl)
    }

    private fun parseMasterPlaylist(content: String, baseUrl: String): Pair<String?, String?> {
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

    private fun parseMediaPlaylist(content: String, baseUrl: String): List<String> =
        content.lineSequence()
            .filter { !it.startsWith("#") && it.isNotBlank() }
            .map { getAbsoluteUrl(baseUrl, it.trim()) }
            .toList()

    private fun getAbsoluteUrl(baseUrl: String, relativeUrl: String): String = URI(baseUrl).resolve(relativeUrl).toString()

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
