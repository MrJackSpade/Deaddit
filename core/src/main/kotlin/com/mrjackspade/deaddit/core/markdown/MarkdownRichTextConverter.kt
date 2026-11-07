package com.mrjackspade.deaddit.core.markdown

import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put

/**
 * Converts markdown text to Reddit's richtext JSON document format.
 *
 * The result is the JSON of Reddit.Api's `RichTextDocument`, which the native bridge's `CommentRichText` /
 * `EditRichText` commands deserialize:
 * `{"document":[{"e":"par","c":[{"e":"text","t":"hi","f":[[1,0,2]]}]}, {"e":"img","id":"key"}, ...]}`.
 * Element keys are `e` (type: par, text, img, link, blockquote, code, raw), `id`, `t`, `c` (children),
 * `u` (url) and `f` (format runs `[code, start, length]`); keys whose value is null are omitted.
 */
object MarkdownRichTextConverter {
    private const val PARAGRAPH = "par"
    private const val TEXT = "text"
    private const val IMAGE = "img"
    private const val LINK = "link"
    private const val BLOCKQUOTE = "blockquote"
    private const val CODE = "code"
    private const val RAW = "raw"

    /** Mirror of Reddit.Api's `RichTextElement`; built mutably like the C# and encoded once at the end. */
    private class Element(val type: String) {
        var id: String? = null
        var text: String? = null
        var children: MutableList<Element>? = null
        var url: String? = null
        var format: List<List<Int>>? = null

        fun toJson(): JsonObject = buildJsonObject {
            put("e", type)
            id?.let { put("id", it) }
            text?.let { put("t", it) }
            children?.let { c -> put("c", JsonArray(c.map { it.toJson() })) }
            url?.let { put("u", it) }
            format?.let { f -> put("f", JsonArray(f.map { run -> JsonArray(run.map(::JsonPrimitive)) })) }
        }
    }

    private class FormatRun(val format: Int, val start: Int, val length: Int)

    private fun textElement(value: String) = Element(TEXT).apply { text = value }

    private fun withChildren(type: String, children: MutableList<Element>) = Element(type).apply { this.children = children }

    fun convert(markdown: String?): JsonObject {
        val document = mutableListOf<Element>()

        if (markdown.isNullOrEmpty()) {
            document.add(withChildren(PARAGRAPH, mutableListOf(textElement(""))))
            return toJson(document)
        }

        // First extract fenced code blocks, which span across paragraph boundaries
        for ((content, isCode) in extractCodeBlocks(markdown)) {
            if (isCode) {
                document.add(withChildren(CODE, mutableListOf(Element(RAW).apply { text = content })))
            } else {
                // Split non-code content into paragraphs on double newline
                for (paragraph in content.split("\n\n")) {
                    addParagraphElements(document, paragraph)
                }
            }
        }

        return toJson(document)
    }

    private fun toJson(document: List<Element>): JsonObject =
        buildJsonObject { put("document", JsonArray(document.map { it.toJson() })) }

    /**
     * Splits markdown into alternating (text, false) and (code, true) segments
     * by extracting fenced code blocks (```...```).
     */
    private fun extractCodeBlocks(markdown: String): List<Pair<String, Boolean>> {
        val blocks = mutableListOf<Pair<String, Boolean>>()
        var i = 0

        while (i < markdown.length) {
            // Look for opening ```
            val fenceStart = markdown.indexOf("```", i)

            if (fenceStart == -1) {
                // No more code blocks, rest is text
                val remaining = markdown.substring(i)

                if (remaining.isNotEmpty()) {
                    blocks.add(remaining to false)
                }

                break
            }

            // Add text before the fence
            if (fenceStart > i) {
                var before = markdown.substring(i, fenceStart)

                // Trim trailing \n\n that precedes the fence
                if (before.endsWith("\n\n")) {
                    before = before.substring(0, before.length - 2)
                }

                if (before.isNotEmpty()) {
                    blocks.add(before to false)
                }
            }

            // Skip past opening ``` and optional language identifier
            val lineEnd = markdown.indexOf('\n', fenceStart)

            if (lineEnd == -1) {
                // Unclosed fence at end of string, treat as text
                blocks.add(markdown.substring(fenceStart) to false)
                break
            }

            val codeStart = lineEnd + 1

            // Find closing ```
            val fenceEnd = markdown.indexOf("\n```", codeStart)

            if (fenceEnd == -1) {
                // Unclosed fence, treat rest as code
                blocks.add(markdown.substring(codeStart) to true)
                break
            }

            blocks.add(markdown.substring(codeStart, fenceEnd) to true)

            // Move past closing ``` and optional trailing newlines
            i = fenceEnd + 4 // "\n```" is 4 chars

            // Skip trailing \n\n after the closing fence
            if (i < markdown.length && markdown[i] == '\n') {
                i++
            }

            if (i < markdown.length && markdown[i] == '\n') {
                i++
            }
        }

        return blocks
    }

    /** Checks if the paragraph is a blockquote (lines starting with >) and handles accordingly. */
    private fun addParagraphElements(document: MutableList<Element>, rawParagraph: String) {
        val trimmed = DotNetText.trimStart(rawParagraph)

        if (trimmed.startsWith('>') && !trimmed.startsWith(">!")) {
            // Strip > prefix from each line to get inner content
            val inner = rawParagraph.split('\n').joinToString("\n") { l ->
                val lt = DotNetText.trimStart(l)

                when {
                    lt.startsWith("> ") -> lt.substring(2)
                    lt.startsWith('>') -> lt.substring(1)
                    else -> l
                }
            }

            val blockquoteChildren = mutableListOf<Element>()

            // Inner content may contain multiple paragraphs separated by \n\n
            for (innerParagraph in inner.split("\n\n")) {
                addParagraphElements(blockquoteChildren, innerParagraph)
            }

            document.add(withChildren(BLOCKQUOTE, blockquoteChildren))
        } else {
            addParagraphContent(document, rawParagraph)
        }
    }

    /**
     * Splits a paragraph around ![image](key) placeholders, adding
     * text paragraphs and image elements directly to the document.
     */
    private fun addParagraphContent(document: MutableList<Element>, paragraph: String) {
        var lastIndex = 0
        var i = 0

        while (i < paragraph.length) {
            // Look for ![image](
            if (paragraph[i] == '!' && i + 9 < paragraph.length && paragraph.regionMatches(i, "![image](", 0, 9)) {
                val closeP = paragraph.indexOf(')', i + 9)

                if (closeP != -1) {
                    // Flush text before the image
                    if (i > lastIndex) {
                        document.add(withChildren(PARAGRAPH, parseInline(paragraph.substring(lastIndex, i))))
                    }

                    document.add(Element(IMAGE).apply { id = paragraph.substring(i + 9, closeP) })

                    i = closeP + 1
                    lastIndex = i
                    continue
                }
            }

            i++
        }

        // Flush remaining text
        if (lastIndex < paragraph.length) {
            document.add(withChildren(PARAGRAPH, parseInline(paragraph.substring(lastIndex))))
        } else if (lastIndex == 0 && paragraph.isEmpty()) {
            document.add(withChildren(PARAGRAPH, mutableListOf(textElement(""))))
        }
    }

    private fun parseInline(text: String): MutableList<Element> {
        val elements = mutableListOf<Element>()

        if (text.isEmpty()) {
            elements.add(textElement(""))
            return elements
        }

        addTextRuns(elements, text)

        if (elements.isEmpty()) {
            elements.add(textElement(""))
        }

        return elements
    }

    /**
     * Parses markdown inline formatting (bold, italic, strikethrough, code, links)
     * into richtext text elements with format arrays.
     */
    private fun addTextRuns(elements: MutableList<Element>, text: String) {
        // Reddit richtext format codes:
        // 1 = bold
        // 2 = italic
        // 8 = strikethrough
        // 32 = superscript
        // 64 = inline code

        val formats = mutableListOf<FormatRun>()
        val plainText = extractFormats(text, formats, elements)

        if (plainText.isEmpty() && elements.isNotEmpty()) {
            return
        }

        val element = textElement(plainText)

        if (formats.isNotEmpty()) {
            element.format = formats.map { listOf(it.format, it.start, it.length) }
        }

        elements.add(element)
    }

    /**
     * Extracts formatting markers from markdown text, returning the plain text
     * and populating the formats list with (formatCode, startIndex, length) tuples.
     * Links are added directly to elements as separate richtext elements.
     */
    private fun extractFormats(markdown: String, formats: MutableList<FormatRun>, elements: MutableList<Element>): String {
        val result = StringBuilder(markdown.length)
        var i = 0

        fun appendRun(inner: String, format: Int) {
            val start = result.length
            result.append(inner)
            formats.add(FormatRun(format, start, inner.length))
        }

        while (i < markdown.length) {
            // Spoiler: >!text!<
            if (markdown[i] == '>' && i + 1 < markdown.length && markdown[i + 1] == '!') {
                val end = markdown.indexOf("!<", i + 2)

                if (end != -1) {
                    appendRun(markdown.substring(i + 2, end), 16)
                    i = end + 2
                    continue
                }
            }

            // Inline code: `code`
            if (markdown[i] == '`') {
                val end = markdown.indexOf('`', i + 1)

                if (end != -1) {
                    appendRun(markdown.substring(i + 1, end), 64)
                    i = end + 1
                    continue
                }
            }

            // Bold+Italic: ***text*** or ___text___
            if (i + 2 < markdown.length &&
                ((markdown[i] == '*' && markdown[i + 1] == '*' && markdown[i + 2] == '*') ||
                    (markdown[i] == '_' && markdown[i + 1] == '_' && markdown[i + 2] == '_'))
            ) {
                val closing = markdown[i].toString().repeat(3)
                val end = markdown.indexOf(closing, i + 3)

                if (end != -1) {
                    val inner = markdown.substring(i + 3, end)
                    val start = result.length
                    result.append(inner)
                    formats.add(FormatRun(1, start, inner.length)) // bold
                    formats.add(FormatRun(2, start, inner.length)) // italic
                    i = end + 3
                    continue
                }
            }

            // Bold: **text** or __text__
            if (i + 1 < markdown.length &&
                ((markdown[i] == '*' && markdown[i + 1] == '*') ||
                    (markdown[i] == '_' && markdown[i + 1] == '_'))
            ) {
                val closing = markdown[i].toString().repeat(2)
                val end = markdown.indexOf(closing, i + 2)

                if (end != -1) {
                    appendRun(markdown.substring(i + 2, end), 1)
                    i = end + 2
                    continue
                }
            }

            // Italic: *text* or _text_
            if (markdown[i] == '*' || markdown[i] == '_') {
                val end = markdown.indexOf(markdown[i], i + 1)

                if (end != -1 && end > i + 1) {
                    appendRun(markdown.substring(i + 1, end), 2)
                    i = end + 1
                    continue
                }
            }

            // Strikethrough: ~~text~~
            if (i + 1 < markdown.length && markdown[i] == '~' && markdown[i + 1] == '~') {
                val end = markdown.indexOf("~~", i + 2)

                if (end != -1) {
                    appendRun(markdown.substring(i + 2, end), 8)
                    i = end + 2
                    continue
                }
            }

            // Superscript: ^(text) or ^word
            if (markdown[i] == '^') {
                if (i + 1 < markdown.length && markdown[i + 1] == '(') {
                    val end = markdown.indexOf(')', i + 2)

                    if (end != -1) {
                        appendRun(markdown.substring(i + 2, end), 32)
                        i = end + 1
                        continue
                    }
                } else if (i + 1 < markdown.length && !DotNetText.isWhiteSpace(markdown[i + 1])) {
                    var end = i + 1

                    while (end < markdown.length && !DotNetText.isWhiteSpace(markdown[end])) {
                        end++
                    }

                    appendRun(markdown.substring(i + 1, end), 32)
                    i = end
                    continue
                }
            }

            // Link: [text](url)
            if (markdown[i] == '[') {
                val closeBracket = markdown.indexOf("](", i + 1)

                if (closeBracket != -1) {
                    val closeParen = markdown.indexOf(')', closeBracket + 2)

                    if (closeParen != -1) {
                        // Flush any accumulated plain text first
                        if (result.isNotEmpty()) {
                            val textEl = textElement(result.toString())

                            if (formats.isNotEmpty()) {
                                textEl.format = formats.map { listOf(it.format, it.start, it.length) }
                                formats.clear()
                            }

                            elements.add(textEl)
                            result.setLength(0)
                        }

                        elements.add(Element(LINK).apply {
                            this.text = markdown.substring(i + 1, closeBracket)
                            url = markdown.substring(closeBracket + 2, closeParen)
                        })

                        i = closeParen + 1
                        continue
                    }
                }
            }

            // Plain character
            result.append(markdown[i])
            i++
        }

        return result.toString()
    }
}
