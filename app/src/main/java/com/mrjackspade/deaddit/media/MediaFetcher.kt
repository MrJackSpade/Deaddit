package com.mrjackspade.deaddit.media

import android.media.MediaCodec
import android.media.MediaExtractor
import android.media.MediaFormat
import android.media.MediaMuxer
import android.os.Build
import com.mrjackspade.deaddit.core.util.DashManifest
import com.mrjackspade.deaddit.core.util.HlsPlaylist
import com.mrjackspade.deaddit.core.util.dotNetRequestUrl
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.File
import java.nio.ByteBuffer
import java.util.UUID

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
                    media.video.inputStream().use { input -> target.outputStream().use { input.copyToCancellable(it) } }
                } finally {
                    media.video.delete()
                    media.audio?.delete()
                }
            }

            ext.equals("mpd", ignoreCase = true) -> fetchMuxedDash(url, target)

            else -> download(url, target)
        }
    }

    /** GETs [url] into [target]; a non-success status is an error. Cancelling the coroutine stops the download. */
    suspend fun download(url: String, target: File) {
        http.fetch(Request.Builder().url(url).build()) { response ->
            if (!response.isSuccessful) throw IllegalStateException("HTTP ${response.code} downloading $url")
            target.outputStream().use { response.body!!.byteStream().copyToCancellable(it) }
        }
    }

    private suspend fun getString(url: String): String =
        http.fetch(Request.Builder().url(url).build()) { response ->
            if (!response.isSuccessful) throw IllegalStateException("HTTP ${response.code} downloading $url")
            response.body!!.string()
        }

    private suspend fun fetchMuxedDash(dashUrl: String, target: File) {
        val manifest = DashManifest.parse(getString(dashUrl))

        val baseUrl = dashUrl.substring(0, dashUrl.lastIndexOf('/') + 1)

        val videoBaseUrl = manifest.videoBaseUrl ?: throw IllegalStateException("No video stream found in DASH manifest")
        val audioBaseUrl = manifest.audioBaseUrl

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

    suspend fun download(m3u8Url: String, tempDir: File): MediaData {
        // Step 1: Download the master playlist content
        val masterPlaylistContent = wrapping("Error downloading master playlist") { getString(m3u8Url) }

        // Step 2: Parse the master playlist to find video and audio playlists
        val (videoPlaylistUrl, audioPlaylistUrl) = HlsPlaylist.parseMaster(masterPlaylistContent, m3u8Url)

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

    /** GETs [url] as .NET's HttpClient sent it: the string parsed and re-escaped by System.Uri. */
    private suspend fun getString(url: String): String =
        http.fetch(Request.Builder().url(dotNetRequestUrl(url)).build()) { response ->
            if (!response.isSuccessful) throw IllegalStateException("HTTP ${response.code} downloading $url")
            response.body!!.string()
        }

    private suspend fun downloadSegments(segmentUrls: List<String>, target: File) {
        target.outputStream().use { output ->
            for ((index, segmentUrl) in segmentUrls.withIndex()) {
                wrapping("Error downloading segment ${index + 1}") {
                    http.fetch(Request.Builder().url(dotNetRequestUrl(segmentUrl)).build()) { response ->
                        if (!response.isSuccessful) throw IllegalStateException("HTTP ${response.code} downloading $segmentUrl")
                        response.body!!.byteStream().copyToCancellable(output)
                    }
                }
            }
        }
    }

    private suspend fun getSegmentUrls(playlistUrl: String): List<String> {
        val playlistContent = wrapping("Error downloading playlist") { getString(playlistUrl) }

        return HlsPlaylist.parseMedia(playlistContent, playlistUrl)
    }

    /** The C#'s `catch (Exception ex) { throw new Exception(what + ": " + ex.Message, ex) }`; a cancellation passes through as is. */
    private suspend fun <T> wrapping(what: String, block: suspend () -> T): T = try {
        block()
    } catch (e: CancellationException) {
        throw e
    } catch (e: Exception) {
        throw Exception("$what: ${e.message}", e)
    }
}