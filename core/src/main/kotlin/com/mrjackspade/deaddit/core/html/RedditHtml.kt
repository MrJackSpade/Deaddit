package com.mrjackspade.deaddit.core.html

import org.jsoup.Jsoup
import org.jsoup.nodes.Element
import org.jsoup.nodes.Node
import org.jsoup.nodes.TextNode

/**
 * Reddit's rendered markdown (body_html / selftext_html) as blocks of styled text, for native
 * rendering. Reddit sends it HTML-escaped inside JSON, wrapped in `<div class="md">`; Reddit.Api's
 * models decode the escaping.
 */
sealed interface Block {
    data class Paragraph(val text: List<Span>) : Block
    data class Heading(val level: Int, val text: List<Span>) : Block
    data class Quote(val blocks: List<Block>) : Block
    data class BulletList(val ordered: Boolean, val start: Int, val items: List<List<Block>>) : Block
    data class Code(val text: String) : Block
    data class Table(val header: List<Cell>, val rows: List<List<Cell>>) : Block
    data object Rule : Block
}

enum class Align { Start, Center, End }

data class Cell(val align: Align, val text: List<Span>)

/** A run of text and the styles on it. [link] is the target of the link it's inside, if any. */
data class Span(
    val text: String,
    val bold: Boolean = false,
    val italic: Boolean = false,
    val strike: Boolean = false,
    val code: Boolean = false,
    val superscript: Int = 0,
    val link: String? = null,
    /** Spoilers are numbered within a body, so each can be revealed on its own. */
    val spoiler: Int? = null,
)

object RedditHtml {
    /** Parses Reddit's HTML, as Reddit.Api's models deliver it: already decoded from the JSON's escaping. */
    fun parse(html: String): List<Block> {
        val body = Jsoup.parseBodyFragment(html).body()
        val root = body.selectFirst("div.md") ?: body
        return Parser().blocks(root)
    }

    private class Parser {
        private var spoilers = 0

        fun blocks(container: Element): List<Block> {
            val result = mutableListOf<Block>()
            val loose = mutableListOf<Node>()

            fun flushLoose() {
                if (loose.isEmpty()) return
                val spans = trim(loose.flatMap { inline(it, Style()) })
                loose.clear()
                if (spans.isNotEmpty()) result += Block.Paragraph(spans)
            }

            for (node in container.childNodes()) {
                if (node is Element && node.tagName() in BLOCK_TAGS) {
                    flushLoose()
                    result += block(node)
                } else {
                    loose += node
                }
            }

            flushLoose()
            return result
        }

        private fun block(element: Element): List<Block> = when (element.tagName()) {
            "p" -> trim(inlines(element)).let { if (it.isEmpty()) emptyList() else listOf(Block.Paragraph(it)) }
            "h1", "h2", "h3", "h4", "h5", "h6" -> listOf(Block.Heading(element.tagName()[1].digitToInt(), trim(inlines(element))))
            "blockquote" -> listOf(Block.Quote(blocks(element)))
            "ul", "ol" -> listOf(Block.BulletList(
                ordered = element.tagName() == "ol",
                start = element.attr("start").toIntOrNull() ?: 1,
                items = element.children().filter { it.tagName() == "li" }.map { blocks(it) },
            ))
            "pre" -> listOf(Block.Code(element.wholeText().trimEnd('\n')))
            "table" -> listOf(table(element))
            "hr" -> listOf(Block.Rule)
            // A wrapper with no meaning of its own: its content stands in its place.
            "div" -> blocks(element)
            else -> throw IllegalArgumentException("Unhandled block element <${element.tagName()}>")
        }

        private fun table(table: Element): Block.Table {
            fun cells(row: Element) = row.children().filter { it.tagName() == "th" || it.tagName() == "td" }.map { cell ->
                val align = when (cell.attr("align").ifEmpty { cell.attr("style").substringAfter("text-align:", "").trim().trimEnd(';') }) {
                    "center" -> Align.Center
                    "right" -> Align.End
                    else -> Align.Start
                }
                Cell(align, trim(inlines(cell)))
            }

            val rows = table.select("tr")
            val headerRow = table.selectFirst("thead tr")
            return Block.Table(
                header = headerRow?.let(::cells) ?: emptyList(),
                rows = rows.filter { it != headerRow }.map(::cells),
            )
        }

        private fun inlines(element: Element): List<Span> = element.childNodes().flatMap { inline(it, Style()) }

        private fun inline(node: Node, style: Style): List<Span> {
            if (node is TextNode) {
                val text = node.wholeText.replace(WHITESPACE, " ")
                return if (text.isEmpty()) emptyList() else listOf(style.span(text))
            }

            if (node !is Element) return emptyList()

            val children = { s: Style -> node.childNodes().flatMap { inline(it, s) } }

            return when (node.tagName()) {
                "strong", "b" -> children(style.copy(bold = true))
                "em", "i" -> children(style.copy(italic = true))
                "del", "s", "strike" -> children(style.copy(strike = true))
                "code" -> listOf(style.copy(code = true).span(node.wholeText()))
                "sup" -> children(style.copy(superscript = style.superscript + 1))
                "a" -> children(style.copy(link = node.attr("href")))
                "br" -> listOf(style.span("\n"))
                "span" -> if (node.hasClass("md-spoiler-text")) children(style.copy(spoiler = spoilers++)) else children(style)
                "img" -> listOf(style.copy(link = style.link ?: node.attr("src")).span(node.attr("alt").ifEmpty { node.attr("src") }))
                // A block element inside an inline context (e.g. a list item's paragraph): keep its text, on its own line.
                in BLOCK_TAGS -> listOf(style.span("\n")) + children(style) + listOf(style.span("\n"))
                else -> children(style)
            }
        }

        /** Drops the whitespace Reddit puts around block content, and merges adjacent runs. */
        private fun trim(spans: List<Span>): List<Span> {
            val merged = mutableListOf<Span>()
            for (span in spans) {
                val last = merged.lastOrNull()
                if (last != null && last.copy(text = "") == span.copy(text = "")) {
                    merged[merged.size - 1] = last.copy(text = last.text + span.text)
                } else {
                    merged += span
                }
            }

            // A line break swallows the source whitespace around it, as in a browser.
            for (i in merged.indices) {
                if (!merged[i].code) merged[i] = merged[i].copy(text = merged[i].text.replace(BREAK_SPACE, "\n"))
            }

            while (merged.isNotEmpty() && merged.first().text.isBlank() && !merged.first().code) merged.removeAt(0)
            while (merged.isNotEmpty() && merged.last().text.isBlank() && !merged.last().code) merged.removeAt(merged.size - 1)
            if (merged.isNotEmpty()) {
                merged[0] = merged[0].let { if (it.code) it else it.copy(text = it.text.trimStart()) }
                merged[merged.size - 1] = merged.last().let { if (it.code) it else it.copy(text = it.text.trimEnd()) }
            }
            return merged
        }

        private data class Style(
            val bold: Boolean = false,
            val italic: Boolean = false,
            val strike: Boolean = false,
            val code: Boolean = false,
            val superscript: Int = 0,
            val link: String? = null,
            val spoiler: Int? = null,
        ) {
            fun span(text: String) = Span(text, bold, italic, strike, code, superscript, link, spoiler)
        }
    }

    private val BLOCK_TAGS = setOf("p", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "ul", "ol", "pre", "table", "hr", "div")

    // Collapses HTML source whitespace (newlines between tags) the way a browser would.
    private val WHITESPACE = Regex("[ \\t\\r\\n]+")

    private val BREAK_SPACE = Regex(" *\n *")
}
