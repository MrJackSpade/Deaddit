package com.mrjackspade.deaddit.media

import android.media.MediaCodec
import android.media.MediaCodecInfo
import android.media.MediaCodecList
import android.media.MediaFormat
import android.media.MediaMuxer
import java.io.File

/** Converts a downloaded file before it's saved or shared (the C# IStreamConverter). */
interface StreamConverter {
    fun canConvert(fileName: String): Boolean
    fun convertFileName(fileName: String): String

    /** Converts [input] into [output]. Blocking; call off the main thread. */
    fun convert(input: File, output: File)
}

/**
 * GIFs to H.264 MP4s (the C# converter, which used FFmpeg's x264). Every frame is encoded at the
 * GIF's own size; encoders that need even or aligned sizes get the canvas padded with black on
 * the right and bottom, never scaled.
 */
class GifToMp4Converter : StreamConverter {
    override fun canConvert(fileName: String) = extensionOf(fileName).equals(".gif", ignoreCase = true)

    /** .NET's Path.ChangeExtension(fileName, ".mp4"). */
    override fun convertFileName(fileName: String): String {
        val extension = extensionOf(fileName)
        return fileName.substring(0, fileName.length - extension.length) + ".mp4"
    }

    override fun convert(input: File, output: File) {
        encode(GifDecoder(input.readBytes()), output)
    }

    private fun encode(decoder: GifDecoder, output: File) {
        // One pass to learn the timing (for the encoder's frame rate), then one to encode.
        val delays = decoder.frames().map { it.delayMs }.toList()
        check(delays.isNotEmpty()) { "The GIF has no frames." }
        val averageDelayMs = delays.sum().toDouble() / delays.size
        val frameRate = maxOf(1, Math.round(1000.0 / averageDelayMs).toInt())

        val codecName = MediaCodecList(MediaCodecList.REGULAR_CODECS).codecInfos
            .firstOrNull { info -> info.isEncoder && info.supportedTypes.any { it.equals(MIME, ignoreCase = true) } }
            ?.name ?: throw IllegalStateException("This device has no H.264 encoder.")

        val codec = MediaCodec.createByCodecName(codecName)
        val muxer = MediaMuxer(output.path, MediaMuxer.OutputFormat.MUXER_OUTPUT_MPEG_4)
        var muxerStarted = false

        try {
            val video = codec.codecInfo.getCapabilitiesForType(MIME).videoCapabilities
            val width = padTo(maxOf(decoder.width, video.supportedWidths.lower), video.widthAlignment)
            val height = padTo(maxOf(decoder.height, video.supportedHeights.lower), video.heightAlignment)
            check(video.isSizeSupported(width, height)) {
                "The H.264 encoder ($codecName) can't encode ${decoder.width}x${decoder.height} (padded to ${width}x$height)."
            }

            // A quarter bit per pixel per frame (high quality for GIF content), within the encoder's range.
            val bitrate = video.bitrateRange.clamp((width.toLong() * height * frameRate / 4).coerceAtMost(Int.MAX_VALUE.toLong()).toInt())

            val format = MediaFormat.createVideoFormat(MIME, width, height).apply {
                setInteger(MediaFormat.KEY_COLOR_FORMAT, MediaCodecInfo.CodecCapabilities.COLOR_FormatYUV420Flexible)
                setInteger(MediaFormat.KEY_BIT_RATE, bitrate)
                setInteger(MediaFormat.KEY_FRAME_RATE, frameRate)
                setInteger(MediaFormat.KEY_I_FRAME_INTERVAL, 1)
            }

            codec.configure(format, null, null, MediaCodec.CONFIGURE_FLAG_ENCODE)
            codec.start()

            val info = MediaCodec.BufferInfo()
            var track = -1

            /** Moves encoded output into the muxer; with [untilEnd], waits for the end of the stream. */
            fun drain(untilEnd: Boolean) {
                while (true) {
                    val index = codec.dequeueOutputBuffer(info, if (untilEnd) -1 else 0)
                    when {
                        index == MediaCodec.INFO_TRY_AGAIN_LATER -> return
                        index == MediaCodec.INFO_OUTPUT_FORMAT_CHANGED -> {
                            check(!muxerStarted) { "The encoder changed its output format twice." }
                            track = muxer.addTrack(codec.outputFormat)
                            muxer.start()
                            muxerStarted = true
                        }
                        index >= 0 -> {
                            val buffer = codec.getOutputBuffer(index)!!
                            if (info.flags and MediaCodec.BUFFER_FLAG_CODEC_CONFIG != 0) info.size = 0
                            if (info.size > 0) {
                                check(muxerStarted) { "The encoder produced output before its format." }
                                buffer.position(info.offset)
                                buffer.limit(info.offset + info.size)
                                muxer.writeSampleData(track, buffer, info)
                            }
                            codec.releaseOutputBuffer(index, false)
                            if (info.flags and MediaCodec.BUFFER_FLAG_END_OF_STREAM != 0) return
                        }
                    }
                }
            }

            /** An input buffer, draining output while the encoder has none free. */
            fun nextInput(): Int {
                while (true) {
                    val index = codec.dequeueInputBuffer(POLL_US)
                    if (index >= 0) return index
                    drain(untilEnd = false)
                }
            }

            var timeUs = 0L
            for (frame in decoder.frames()) {
                val index = nextInput()
                // The buffer's size, taken before the Image view of it (which replaces the buffer view).
                val size = codec.getInputBuffer(index)!!.capacity()
                val image = codec.getInputImage(index)!!
                writeYuv(frame.pixels, decoder.width, decoder.height, image)
                codec.queueInputBuffer(index, 0, size, timeUs, 0)
                timeUs += frame.delayMs * 1000L
                drain(untilEnd = false)
            }

            codec.queueInputBuffer(nextInput(), 0, 0, timeUs, MediaCodec.BUFFER_FLAG_END_OF_STREAM)
            drain(untilEnd = true)

            codec.stop()
            muxer.stop()
        } finally {
            codec.release()
            muxer.release()
        }
    }

    /** BT.601 limited-range YUV 4:2:0 from ARGB; padding outside the GIF's canvas is black. */
    private fun writeYuv(argb: IntArray, gifWidth: Int, gifHeight: Int, image: android.media.Image) {
        val width = image.width
        val height = image.height
        val yPlane = image.planes[0]
        val uPlane = image.planes[1]
        val vPlane = image.planes[2]
        val yBuffer = yPlane.buffer
        val uBuffer = uPlane.buffer
        val vBuffer = vPlane.buffer

        fun pixel(x: Int, y: Int): Int = if (x < gifWidth && y < gifHeight) argb[y * gifWidth + x] else 0

        for (y in 0 until height) {
            val row = y * yPlane.rowStride
            for (x in 0 until width) {
                val p = pixel(x, y)
                val r = (p shr 16) and 0xFF
                val g = (p shr 8) and 0xFF
                val b = p and 0xFF
                yBuffer.put(row + x * yPlane.pixelStride, (((66 * r + 129 * g + 25 * b + 128) shr 8) + 16).toByte())
            }
        }

        for (cy in 0 until height / 2) {
            for (cx in 0 until width / 2) {
                var r = 0
                var g = 0
                var b = 0
                for (dy in 0..1) for (dx in 0..1) {
                    val p = pixel(cx * 2 + dx, cy * 2 + dy)
                    r += (p shr 16) and 0xFF
                    g += (p shr 8) and 0xFF
                    b += p and 0xFF
                }
                r /= 4
                g /= 4
                b /= 4
                val u = ((-38 * r - 74 * g + 112 * b + 128) shr 8) + 128
                val v = ((112 * r - 94 * g - 18 * b + 128) shr 8) + 128
                uBuffer.put(cy * uPlane.rowStride + cx * uPlane.pixelStride, u.toByte())
                vBuffer.put(cy * vPlane.rowStride + cx * vPlane.pixelStride, v.toByte())
            }
        }
    }

    private fun padTo(value: Int, alignment: Int): Int {
        val step = maxOf(2, alignment)
        return (value + step - 1) / step * step
    }

    private companion object {
        const val MIME = MediaFormat.MIMETYPE_VIDEO_AVC

        /** How long each wait for a free input buffer lasts before output is drained again. It loops; it never gives up. */
        const val POLL_US = 10_000L

        /** .NET's Path.GetExtension: from the last '.' after the last '/', or "" when none or trailing. */
        fun extensionOf(fileName: String): String {
            val dot = fileName.lastIndexOf('.')
            if (dot < 0 || dot < fileName.lastIndexOf('/') || dot == fileName.length - 1) return ""
            return fileName.substring(dot)
        }
    }
}
