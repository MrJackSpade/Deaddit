package com.mrjackspade.deaddit.media

/**
 * Decodes an animated GIF into composited full-canvas frames. Transparent pixels are left as 0
 * (black, once alpha is dropped), as FFmpeg's GIF decoder gave the C# converter.
 */
class GifDecoder(private val data: ByteArray) {
    /** One frame: the whole canvas after drawing it ([pixels], ARGB, reused between frames) and how long it shows. */
    class Frame(val pixels: IntArray, val delayMs: Int)

    private var pos = 0

    val width: Int
    val height: Int
    private val globalColorTable: IntArray?

    init {
        val signature = String(data, 0, 6, Charsets.US_ASCII)
        require(signature == "GIF87a" || signature == "GIF89a") { "Not a GIF (signature '$signature')." }
        pos = 6
        width = u16()
        height = u16()
        val packed = u8()
        u8() // background color index: browsers and FFmpeg clear disposed areas to transparent, not to this.
        u8() // pixel aspect ratio
        globalColorTable = if (packed and 0x80 != 0) colorTable(2 shl (packed and 7)) else null
        require(width > 0 && height > 0) { "GIF has an empty canvas (${width}x$height)." }
    }

    /** The frames, in order. Each yielded frame's pixels are valid until the next is requested. */
    fun frames(): Sequence<Frame> = sequence {
        val canvas = IntArray(width * height)
        var saved: IntArray? = null

        var disposal = 0
        var transparentIndex = -1
        var delayCs = 0

        // The previous frame's disposal, applied before the next frame draws.
        var pendingDisposal = 0
        var previousRect = IntArray(4)

        while (true) {
            // A GIF missing its trailer ends after its last complete block, as browsers and FFmpeg read it.
            if (pos >= data.size) break

            when (val introducer = u8()) {
                0x21 -> {
                    val label = u8()
                    if (label == 0xF9) {
                        val size = u8()
                        check(size >= 4) { "GIF graphic control extension is $size bytes; it must be 4." }
                        val packed = u8()
                        disposal = (packed shr 2) and 7
                        delayCs = u16()
                        val transparent = u8()
                        transparentIndex = if (packed and 1 != 0) transparent else -1
                        pos += size - 4
                        skipSubBlocks()
                    } else {
                        skipSubBlocks()
                    }
                }

                0x2C -> {
                    val left = u16()
                    val top = u16()
                    val frameWidth = u16()
                    val frameHeight = u16()
                    val packed = u8()
                    val interlaced = packed and 0x40 != 0
                    val colors = if (packed and 0x80 != 0) colorTable(2 shl (packed and 7)) else globalColorTable
                        ?: throw IllegalStateException("GIF frame has no color table, local or global.")

                    val indices = decodeLzw(frameWidth * frameHeight)

                    // Dispose of the previous frame.
                    when (pendingDisposal) {
                        2 -> clear(canvas, previousRect)
                        3 -> saved?.copyInto(canvas)
                    }

                    saved = if (disposal == 3) canvas.copyOf() else null

                    draw(canvas, indices, colors, left, top, frameWidth, frameHeight, interlaced, transparentIndex)

                    // FFmpeg's GIF demuxer: delays under 2/100s play at 10/100s.
                    val delay = if (delayCs < 2) 10 else delayCs
                    yield(Frame(canvas, delay * 10))

                    pendingDisposal = disposal
                    previousRect = intArrayOf(left, top, frameWidth, frameHeight)
                    disposal = 0
                    transparentIndex = -1
                    delayCs = 0
                }

                0x3B -> break

                else -> throw IllegalStateException("Unexpected GIF block 0x${introducer.toString(16)} at byte ${pos - 1}.")
            }
        }
    }

    private fun clear(canvas: IntArray, rect: IntArray) {
        val (left, top, w, h) = rect.toList()
        for (y in top until minOf(top + h, height)) {
            for (x in left until minOf(left + w, width)) {
                canvas[y * width + x] = 0
            }
        }
    }

    private fun draw(
        canvas: IntArray, indices: ByteArray, colors: IntArray,
        left: Int, top: Int, w: Int, h: Int, interlaced: Boolean, transparentIndex: Int,
    ) {
        val rows = if (interlaced) interlacedRows(h) else IntArray(h) { it }

        for (sourceRow in 0 until h) {
            val y = top + rows[sourceRow]
            if (y >= height) continue
            for (col in 0 until w) {
                val x = left + col
                if (x >= width) continue
                val index = indices[sourceRow * w + col].toInt() and 0xFF
                if (index == transparentIndex) continue
                check(index < colors.size) { "GIF pixel uses color $index of a ${colors.size}-color table." }
                canvas[y * width + x] = colors[index]
            }
        }
    }

    /** For each row in data order, the row of the image it belongs to. */
    private fun interlacedRows(h: Int): IntArray {
        val rows = IntArray(h)
        var i = 0
        for ((start, step) in listOf(0 to 8, 4 to 8, 2 to 4, 1 to 2)) {
            var row = start
            while (row < h) {
                rows[i++] = row
                row += step
            }
        }
        return rows
    }

    private fun decodeLzw(pixelCount: Int): ByteArray {
        val minCodeSize = u8()
        check(minCodeSize in 1..11) { "GIF LZW minimum code size $minCodeSize is out of range." }
        val bytes = readSubBlocks()

        val output = ByteArray(pixelCount)
        var outPos = 0

        val clearCode = 1 shl minCodeSize
        val endCode = clearCode + 1
        val prefix = IntArray(4096)
        val suffix = ByteArray(4096)
        val stack = ByteArray(4097)
        for (i in 0 until clearCode) suffix[i] = i.toByte()

        var codeSize = minCodeSize + 1
        var nextCode = endCode + 1
        var oldCode = -1
        var first = 0

        var bitBuffer = 0
        var bitCount = 0
        var bytePos = 0

        while (outPos < pixelCount) {
            while (bitCount < codeSize) {
                check(bytePos < bytes.size) { "GIF image data ends after $outPos of $pixelCount pixels." }
                bitBuffer = bitBuffer or ((bytes[bytePos++].toInt() and 0xFF) shl bitCount)
                bitCount += 8
            }
            var code = bitBuffer and ((1 shl codeSize) - 1)
            bitBuffer = bitBuffer ushr codeSize
            bitCount -= codeSize

            if (code == clearCode) {
                codeSize = minCodeSize + 1
                nextCode = endCode + 1
                oldCode = -1
                continue
            }
            if (code == endCode) {
                check(outPos == pixelCount) { "GIF image data ends after $outPos of $pixelCount pixels." }
                break
            }

            if (oldCode == -1) {
                check(code < clearCode) { "GIF image data starts with undefined code $code." }
                output[outPos++] = suffix[code]
                first = code
                oldCode = code
                continue
            }

            val inCode = code
            var top = 0
            if (code >= nextCode) {
                check(code == nextCode) { "GIF image data uses undefined code $code." }
                stack[top++] = first.toByte()
                code = oldCode
            }
            while (code >= clearCode) {
                stack[top++] = suffix[code]
                code = prefix[code]
            }
            first = suffix[code].toInt() and 0xFF
            stack[top++] = first.toByte()

            if (nextCode < 4096) {
                prefix[nextCode] = oldCode
                suffix[nextCode] = first.toByte()
                nextCode++
                if (nextCode == (1 shl codeSize) && codeSize < 12) codeSize++
            }
            oldCode = inCode

            while (top > 0 && outPos < pixelCount) {
                output[outPos++] = stack[--top]
            }
        }

        return output
    }

    private fun colorTable(size: Int): IntArray = IntArray(size) {
        val r = u8()
        val g = u8()
        val b = u8()
        (0xFF shl 24) or (r shl 16) or (g shl 8) or b
    }

    private fun readSubBlocks(): ByteArray {
        val out = java.io.ByteArrayOutputStream()
        while (true) {
            val size = u8()
            if (size == 0) break
            check(pos + size <= data.size) { "GIF data block runs past the end of the file." }
            out.write(data, pos, size)
            pos += size
        }
        return out.toByteArray()
    }

    private fun skipSubBlocks() {
        while (true) {
            val size = u8()
            if (size == 0) break
            pos += size
        }
    }

    private fun u8(): Int {
        check(pos < data.size) { "GIF data ends unexpectedly at byte $pos." }
        return data[pos++].toInt() and 0xFF
    }

    private fun u16(): Int = u8() or (u8() shl 8)
}
