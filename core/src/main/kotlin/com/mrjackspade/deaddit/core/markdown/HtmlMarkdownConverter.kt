package com.mrjackspade.deaddit.core.markdown

/** Converts Reddit body_html to markdown text suitable for editing. */
object HtmlMarkdownConverter {
    fun convert(html: String?): String {
        if (html.isNullOrEmpty()) {
            return ""
        }

        val document = HapHtml.parse(html)

        // Reddit wraps body_html in <div class="md">...</div>
        val root = document.descendantElements().firstOrNull { e ->
            e.name == "div" && e.attributes.any { it.name == "class" && it.value == "md" }
        } ?: document

        val sb = StringBuilder()
        processChildren(root, sb)

        // Trim trailing newlines
        trimTrailingNewlines(sb)

        return sb.toString()
    }

    private fun trimTrailingNewlines(sb: StringBuilder) {
        while (sb.isNotEmpty() && sb[sb.length - 1] == '\n') {
            sb.setLength(sb.length - 1)
        }
    }

    private fun endsWithNewline(sb: StringBuilder) = sb[sb.length - 1] == '\n'

    private fun processChildren(parent: HapElement, sb: StringBuilder) {
        for (child in parent.children) {
            processNode(child, sb)
        }
    }

    private fun quoteDepth(line: String): Int {
        var depth = 0
        var i = 0

        while (i + 1 < line.length && line[i] == '>' && line[i + 1] == ' ') {
            depth++
            i += 2
        }

        // Also count a trailing ">" with no space (e.g. the empty-quote line ">")
        if (i < line.length && line[i] == '>' && i + 1 == line.length) {
            depth++
        }

        return depth
    }

    private fun isEmptyQuoteLine(line: String): Boolean {
        // True for "> ", "> > ", "> > >", etc. — lines that are only `>` markers and whitespace.
        for (c in line) {
            if (c != '>' && c != ' ') {
                return false
            }
        }

        return true
    }

    private fun processNode(node: HapNode, sb: StringBuilder) {
        when (node) {
            is HapText -> {
                val text = DotNetText.htmlDecode(node.raw)!!

                // Skip whitespace-only text nodes between block elements
                // (insignificant HTML formatting whitespace from Reddit's body_html)
                if (DotNetText.isNullOrWhiteSpace(text) && isBlockContainer(node.parent)) {
                    return
                }

                sb.append(text)
            }

            is HapElement -> processElement(node, sb)
            is HapComment -> {}
        }
    }

    private fun processElement(node: HapElement, sb: StringBuilder) {
        when (node.name) {
            "p" -> {
                if (sb.isNotEmpty() && !endsWithNewline(sb)) {
                    sb.append("\n\n")
                }

                processChildren(node, sb)
                sb.append("\n\n")
            }

            "br" -> sb.append('\n')

            "strong", "b" -> {
                sb.append("**")
                processChildren(node, sb)
                sb.append("**")
            }

            "em", "i" -> {
                sb.append('*')
                processChildren(node, sb)
                sb.append('*')
            }

            "del", "s" -> {
                sb.append("~~")
                processChildren(node, sb)
                sb.append("~~")
            }

            "sup" -> {
                sb.append("^(")
                processChildren(node, sb)
                sb.append(')')
            }

            "a" -> {
                val href = node.attribute("href")
                val linkText = getInnerText(node)

                if (href != null) {
                    // Bare URL (link text matches href)
                    if (linkText == href || linkText == DotNetText.htmlDecode(href)) {
                        sb.append(linkText)
                    } else {
                        sb.append('[')
                        sb.append(linkText)
                        sb.append("](")
                        sb.append(href)
                        sb.append(')')
                    }
                } else {
                    sb.append(linkText)
                }
            }

            "code" -> {
                // Check if inside a <pre> (block code) — handled by pre case
                if (node.parent?.name == "pre") {
                    sb.append(DotNetText.htmlDecode(node.innerText()))
                } else {
                    sb.append('`')
                    sb.append(DotNetText.htmlDecode(node.innerText()))
                    sb.append('`')
                }
            }

            "pre" -> {
                if (sb.isNotEmpty() && !endsWithNewline(sb)) {
                    sb.append("\n\n")
                }

                sb.append("```\n")
                processChildren(node, sb)
                sb.append("\n```\n\n")
            }

            "blockquote" -> {
                if (sb.isNotEmpty() && !endsWithNewline(sb)) {
                    sb.append("\n\n")
                }

                val inner = StringBuilder()
                processChildren(node, inner)

                // Trim trailing newlines from inner content
                trimTrailingNewlines(inner)

                val prefixed = inner.toString().split('\n').map { "> $it" }.toMutableList()

                // Reddit's parser drops deeper-nested content when quote depth drops by
                // more than one level across a paragraph break. The empty-quote separator
                // (e.g. "> " after stripping one level) becomes a blank line that splits
                // the next paragraph out of the nested blockquote entirely. Bump the
                // separator AND the next content line up to depth (deepDepth - 1) so the
                // parser stays inside the inner blockquote when re-parsing.
                var i = 0
                while (i + 2 < prefixed.size) {
                    val deepDepth = quoteDepth(prefixed[i])
                    val sepDepth = quoteDepth(prefixed[i + 1])
                    val nextDepth = quoteDepth(prefixed[i + 2])

                    if (isEmptyQuoteLine(prefixed[i + 1]) && deepDepth - nextDepth >= 2) {
                        val bumpedDepth = deepDepth - 1
                        // Enumerable.Repeat throws on a negative count.
                        val count = bumpedDepth - sepDepth
                        require(count >= 0) { "count ('$count') must be a non-negative value." }
                        val bumpPrefix = "> ".repeat(count)
                        prefixed[i + 1] = bumpPrefix + prefixed[i + 1]
                        prefixed[i + 2] = bumpPrefix + prefixed[i + 2]
                    }

                    i++
                }

                for (line in prefixed) {
                    sb.append(line)
                    sb.append('\n')
                }

                sb.append('\n')
            }

            "table" -> {
                if (sb.isNotEmpty() && !endsWithNewline(sb)) {
                    sb.append("\n\n")
                }

                processTable(node, sb)
                sb.append('\n')
            }

            // Handled by processTable
            "thead", "tbody", "tr", "th", "td" -> {}

            "span" -> {
                // Spoiler spans: <span class="md-spoiler-text">
                val cls = node.attribute("class")

                if (cls != null && cls.contains("spoiler")) {
                    sb.append(">!")
                    processChildren(node, sb)
                    sb.append("!<")
                } else {
                    processChildren(node, sb)
                }
            }

            "div" -> processChildren(node, sb)

            "hr" -> {
                if (sb.isNotEmpty() && !endsWithNewline(sb)) {
                    sb.append("\n\n")
                }

                sb.append("---\n\n")
            }

            "ul", "ol" -> {
                if (sb.isNotEmpty() && !endsWithNewline(sb)) {
                    sb.append("\n\n")
                }

                processList(node, sb)
                sb.append('\n')
            }

            // Handled by processList
            "li" -> {}

            "h1", "h2", "h3", "h4", "h5", "h6" -> {
                if (sb.isNotEmpty() && !endsWithNewline(sb)) {
                    sb.append("\n\n")
                }

                val level = node.name[1] - '0'
                sb.append("#".repeat(level))
                sb.append(' ')
                processChildren(node, sb)
                sb.append("\n\n")
            }

            else -> processChildren(node, sb)
        }
    }

    private fun processTable(tableNode: HapElement, sb: StringBuilder) {
        val rows = mutableListOf<List<String>>()

        for (section in tableNode.children) {
            if (section !is HapElement) {
                continue
            }

            for (tr in section.children) {
                if (tr !is HapElement || tr.name != "tr") {
                    continue
                }

                val cells = tr.children
                    .filter { it is HapElement && (it.name == "th" || it.name == "td") }
                    .map { DotNetText.trim(getInnerText(it as HapElement)) }

                rows.add(cells)
            }

            // Add separator after thead
            if (section.name == "thead" && rows.isNotEmpty()) {
                val colCount = rows[rows.size - 1].size
                rows.add(List(colCount) { "---" })
            }
        }

        for (row in rows) {
            sb.append("| ")
            sb.append(row.joinToString(" | "))
            sb.append(" |\n")
        }
    }

    private fun processList(listNode: HapElement, sb: StringBuilder) {
        val ordered = listNode.name == "ol"
        var index = 1

        for (li in listNode.children) {
            if (li !is HapElement || li.name != "li") {
                continue
            }

            if (ordered) {
                sb.append("$index. ")
                index++
            } else {
                sb.append("- ")
            }

            val liContent = StringBuilder()
            processChildren(li, liContent)

            // Trim trailing newlines from li content
            trimTrailingNewlines(liContent)

            sb.append(liContent)
            sb.append('\n')
        }
    }

    private fun getInnerText(node: HapElement): String {
        val sb = StringBuilder()
        processChildren(node, sb)
        return sb.toString()
    }

    private fun isBlockContainer(node: HapElement?): Boolean {
        if (node == null) {
            return false
        }

        return when (node.name) {
            "div", "blockquote", "ul", "ol", "li", "table", "thead", "tbody", "tr" -> true
            else -> false
        }
    }
}
