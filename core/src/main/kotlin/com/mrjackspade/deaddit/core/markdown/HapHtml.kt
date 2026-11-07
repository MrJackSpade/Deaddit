package com.mrjackspade.deaddit.core.markdown

/**
 * The HTML tree HtmlAgilityPack 1.12.4 builds, which the C# HtmlMarkdownConverter walked.
 *
 * HtmlAgilityPack is a tag-soup parser, not an HTML5 one: it builds no implied html/head/body/tbody, keeps text
 * and attribute values exactly as written (entities NOT decoded), and has its own rules for void elements,
 * raw-text elements, implicit closes and malformed tags. jsoup follows HTML5 and differs on all of those (e.g. a
 * `<tr>` straight under `<table>`, the newline after `<pre>`, entities in `href`), so the converter would not
 * produce the same markdown on it. This parser reproduces HtmlAgilityPack's observable behaviour instead; it was
 * derived from and checked against the real library (differential runs over generated Reddit-style and
 * malformed HTML).
 */
internal sealed class HapNode {
    var parent: HapElement? = null
}

/** A text node; [raw] is the source text, entities not decoded. */
internal class HapText(val raw: String) : HapNode()

internal class HapComment : HapNode()

internal class HapElement(
    /** Lower-cased tag name; "#document" for the root. */
    val name: String,
    val attributes: List<HapAttribute> = emptyList(),
) : HapNode() {
    val children = mutableListOf<HapNode>()

    /** Set on script/style: HtmlAgilityPack's InnerText leaves their text out. */
    var hideInnerText = false

    fun append(child: HapNode) {
        child.parent = this
        children.add(child)
    }

    /** HtmlAgilityPack's `GetAttributeValue(name, null)`: the last attribute with that name wins. */
    fun attribute(name: String): String? = attributes.lastOrNull { it.name == name }?.value

    /** HtmlAgilityPack's `InnerText`: raw text of descendant text nodes, skipping comments and script/style. */
    fun innerText(): String = StringBuilder().also { appendInnerText(it) }.toString()

    private fun appendInnerText(sb: StringBuilder) {
        for (child in children) {
            when (child) {
                is HapText -> sb.append(child.raw)
                is HapElement -> if (!child.hideInnerText) child.appendInnerText(sb)
                is HapComment -> {}
            }
        }
    }

    /** Pre-order descendants (excluding this node). */
    fun descendantElements(): Sequence<HapElement> = sequence {
        for (child in children) {
            if (child is HapElement) {
                yield(child)
                yieldAll(child.descendantElements())
            }
        }
    }
}

/** [name] is lower-cased; [value] is the raw source text ("" when the attribute has no complete value). */
internal class HapAttribute(val name: String, val value: String)

internal object HapHtml {
    /** Elements HtmlAgilityPack treats as never having content. */
    private val EMPTY_ELEMENTS = setOf(
        "base", "link", "meta", "isindex", "hr", "col", "img", "param", "embed", "frame", "wbr", "bgsound",
        "spacer", "keygen", "area", "input", "basefont", "source", "br",
    )

    /** Raw-text elements: content runs to the matching end tag. */
    private val CDATA_ELEMENTS = setOf("script", "style", "noxhtml", "textarea", "title")

    /** Tags whose block start implicitly ends an open `<p>`. */
    private val P_CLOSERS = setOf(
        "address", "article", "aside", "blockquote", "dir", "div", "dl", "fieldset", "footer", "form", "h1", "h2",
        "h3", "h4", "h5", "h6", "header", "hr", "li", "menu", "nav", "ol", "p", "pre", "section", "table", "ul",
    )

    fun parse(html: String): HapElement = Parser(html).run()

    private fun isEmptyElement(name: String) = name.isEmpty() || name[0] == '!' || name[0] == '?' || name in EMPTY_ELEMENTS

    private fun isCDataElement(name: String) = name in CDATA_ELEMENTS

    /** True when starting [child] closes the open [parent] (HtmlAgilityPack's implicit and explicit ends). */
    private fun startClosesParent(parent: String, child: String): Boolean = when (parent) {
        "a" -> child == "a"
        "dd", "dt" -> child == "dt" || child == "dd"
        "li" -> child == "li"
        "p" -> child in P_CLOSERS
        "option" -> child == "option"
        "title" -> child == "title"
        "table" -> child == "table"
        "caption" -> child == "colgroup" || child == "tbody" || child == "tfoot" || child == "thead" || child == "tr"
        "colgroup" -> child == "tbody" || child == "tfoot" || child == "thead" || child == "tr"
        "thead", "tbody" -> child == "tbody" || child == "tfoot"
        "tr" -> child == "tr" || child == "tbody" || child == "tfoot" || child == "thead"
        "td", "th" -> child == "td" || child == "th" || child == "tr" || child == "tbody" || child == "tfoot"
        "h1" -> child == "h2" || child == "h3" || child == "h4" || child == "h5"
        "h2" -> child == "h1" || child == "h3" || child == "h4" || child == "h5"
        "h3" -> child == "h1" || child == "h2" || child == "h4" || child == "h5"
        "h4" -> child == "h1" || child == "h2" || child == "h3" || child == "h5"
        "h5" -> child == "h1" || child == "h2" || child == "h3" || child == "h4"
        else -> false
    }

    /** HtmlAgilityPack's tag-syntax whitespace. */
    private fun isWhite(c: Char) = c == '\n' || c == '\r' || c == ' ' || c == '\t'

    private val DOTTED_CAPITAL_I = Char(0x130)
    private val DOTLESS_SMALL_I = Char(0x131)

    /** .NET `char.ToLowerInvariant`: simple case mapping, except that U+0130 is left alone. */
    private fun lower(c: Char): Char = if (c == DOTTED_CAPITAL_I) c else Character.toLowerCase(c)

    /** .NET `char.ToUpperInvariant`: simple case mapping, except that U+0131 is left alone. */
    private fun upper(c: Char): Char = if (c == DOTLESS_SMALL_I) c else Character.toUpperCase(c)

    /** .NET `ToLowerInvariant` (length-preserving). */
    private fun lower(s: String): String {
        val chars = CharArray(s.length) { lower(s[it]) }
        return String(chars)
    }

    /** .NET `OrdinalIgnoreCase` for one pair: an ASCII character only ever matches ASCII (so 'ſ' is not 's'). */
    private fun equalsIgnoreCase(a: Char, b: Char): Boolean {
        if (a == b) {
            return true
        }

        if (a.code < 0x80 || b.code < 0x80) {
            return a.code < 0x80 && b.code < 0x80 && a.uppercaseChar() == b.uppercaseChar()
        }

        return upper(a) == upper(b)
    }

    /** .NET `OrdinalIgnoreCase` comparison of a region of [text] with [other]. */
    private fun regionEqualsIgnoreCase(text: String, start: Int, other: String): Boolean {
        for (k in other.indices) {
            if (!equalsIgnoreCase(text[start + k], other[k])) {
                return false
            }
        }

        return true
    }

    private enum class State {
        Text, Tag, BetweenAttributes, EmptyTag, AttributeName, AttributeBeforeEquals, AttributeAfterEquals,
        AttributeValue, QuotedAttributeValue, Comment, ServerSideCode, PcData,
    }

    private class PendingAttribute(val nameStart: Int) {
        var nameEnd = -1
        var valueStart = -1
        var valueEnd = -1
    }

    private class Parser(private val text: String) {
        private val document = HapElement("#document")
        private val stack = ArrayDeque<HapElement>().apply { addLast(document) }
        private val top get() = stack.last()

        private var p = 0
        private var state = State.Text
        private var oldState = State.Text
        private var textStart = 0

        // The tag being read.
        private var tagStart = 0
        private var isStartTag = false
        private var nameStart = 0
        private var nameEnd = -1
        private var attributes = mutableListOf<PendingAttribute>()
        private var quote = '"'
        private var fullComment = false

        // The open raw-text element.
        private var cdataStart = 0

        fun run(): HapElement {
            while (p < text.length) {
                step()
            }

            // Only a text run in progress survives the end of input; an unfinished tag, comment or
            // raw-text body is dropped.
            if (state == State.Text || (state == State.ServerSideCode && oldState == State.Text)) {
                appendText(text.length)
            }

            return document
        }

        private fun appendText(end: Int) {
            if (end > textStart) {
                top.append(HapText(text.substring(textStart, end)))
            }
        }

        private fun beginText(at: Int) {
            state = State.Text
            textStart = at
        }

        private fun tagName(): String = if (nameEnd < 0) "" else lower(text.substring(nameStart, nameEnd))

        private fun startAttribute(at: Int) {
            attributes.add(PendingAttribute(at))
        }

        private val attribute get() = attributes.last()

        private fun step() {
            val c = text[p]

            when (state) {
                State.Text -> {
                    if (newCheck()) return
                    p++
                }

                State.Tag -> {
                    if (newCheck()) return

                    when {
                        isWhite(c) -> {
                            endTagName()
                            state = State.BetweenAttributes
                            p++
                        }

                        c == '/' -> {
                            endTagName()
                            state = State.EmptyTag
                            p++
                        }

                        c == '>' -> {
                            endTagName()
                            p++
                            finishTag(close = false, end = p)
                        }

                        else -> p++
                    }
                }

                State.BetweenAttributes -> {
                    if (newCheck()) return

                    when {
                        isWhite(c) -> p++
                        c == '/' || c == '?' -> {
                            state = State.EmptyTag
                            p++
                        }

                        c == '>' -> {
                            p++
                            finishTag(close = false, end = p)
                        }

                        else -> {
                            startAttribute(p)
                            state = State.AttributeName
                            p++
                        }
                    }
                }

                State.EmptyTag -> {
                    if (newCheck()) return

                    when {
                        c == '>' -> {
                            p++
                            finishTag(close = true, end = p)
                        }

                        // Attributes wrongly separated by '/': read the character again as an attribute start.
                        !isWhite(c) -> state = State.BetweenAttributes
                        else -> {
                            state = State.BetweenAttributes
                            p++
                        }
                    }
                }

                State.AttributeName -> {
                    if (newCheck()) return

                    when {
                        isWhite(c) -> {
                            attribute.nameEnd = p
                            state = State.AttributeBeforeEquals
                            p++
                        }

                        c == '=' -> {
                            attribute.nameEnd = p
                            state = State.AttributeAfterEquals
                            p++
                        }

                        c == '>' -> {
                            attribute.nameEnd = p
                            p++
                            finishTag(close = false, end = p)
                        }

                        else -> p++
                    }
                }

                State.AttributeBeforeEquals -> {
                    if (newCheck()) return

                    when {
                        isWhite(c) -> p++
                        c == '>' -> {
                            p++
                            finishTag(close = false, end = p)
                        }

                        c == '=' -> {
                            state = State.AttributeAfterEquals
                            p++
                        }

                        // No '=': a new attribute starts at this character.
                        else -> state = State.BetweenAttributes
                    }
                }

                State.AttributeAfterEquals -> {
                    if (newCheck()) return

                    when {
                        isWhite(c) -> p++
                        c == '\'' || c == '"' -> {
                            state = State.QuotedAttributeValue
                            attribute.valueStart = p + 1
                            quote = c
                            p++
                        }

                        c == '>' -> {
                            p++
                            finishTag(close = false, end = p)
                        }

                        else -> {
                            attribute.valueStart = p
                            state = State.AttributeValue
                            p++
                        }
                    }
                }

                State.AttributeValue -> {
                    if (newCheck()) return

                    when {
                        isWhite(c) -> {
                            attribute.valueEnd = p
                            state = State.BetweenAttributes
                            p++
                        }

                        c == '>' -> {
                            attribute.valueEnd = p
                            p++
                            finishTag(close = false, end = p)
                        }

                        else -> p++
                    }
                }

                State.QuotedAttributeValue -> {
                    when {
                        c == quote -> {
                            attribute.valueEnd = p
                            state = State.BetweenAttributes
                            p++
                        }

                        c == '<' && p + 1 < text.length && text[p + 1] == '%' -> {
                            oldState = state
                            state = State.ServerSideCode
                            p++
                        }

                        else -> p++
                    }
                }

                State.Comment -> {
                    if (c == '>') {
                        val ended = !fullComment ||
                            (text[p - 1] == '-' && text[p - 2] == '-') ||
                            (text[p - 1] == '!' && text[p - 2] == '-' && text[p - 3] == '-')

                        if (ended) {
                            top.append(HapComment())
                            p++
                            beginText(p)
                            return
                        }
                    }

                    p++
                }

                State.ServerSideCode -> {
                    if (c == '%' && p + 1 < text.length && text[p + 1] == '>') {
                        when (oldState) {
                            State.AttributeAfterEquals -> state = State.AttributeValue
                            State.BetweenAttributes -> {
                                attribute.nameEnd = p + 2
                                state = State.BetweenAttributes
                            }

                            else -> state = oldState
                        }

                        p += 2
                    } else if (oldState == State.QuotedAttributeValue && c == quote) {
                        // Read the quote again: it ends the value.
                        state = oldState
                    } else {
                        p++
                    }
                }

                State.PcData -> {
                    val element = top
                    val name = element.name

                    if (name.length + 3 <= text.length - p &&
                        regionEqualsIgnoreCase(text, p, "</$name")
                    ) {
                        val after = text[p + 2 + name.length]

                        if (after == '>' || isWhite(after)) {
                            element.append(HapText(text.substring(cdataStart, p)))

                            if (name == "script" || name == "style") {
                                element.hideInnerText = true
                            }

                            beginTag(at = p, start = false)
                            return
                        }
                    }

                    p++
                }
            }
        }

        /** Starts reading a tag whose '<' is at [at]. */
        private fun beginTag(at: Int, start: Boolean) {
            tagStart = at
            isStartTag = start
            nameStart = if (start) at + 1 else at + 2
            nameEnd = -1
            attributes = mutableListOf()
            state = State.Tag
            p = nameStart
        }

        /**
         * HtmlAgilityPack's NewCheck: a '<' that starts server code, a comment or a tag ends whatever was being
         * read. Returns true when it did (and has moved on), false when the character is ordinary.
         */
        private fun newCheck(): Boolean {
            if (text[p] != '<') {
                return false
            }

            val next = if (p + 1 < text.length) text[p + 1] else null

            if (next == '%') {
                when (state) {
                    State.AttributeAfterEquals -> attribute.valueStart = p
                    State.BetweenAttributes -> startAttribute(p)
                    else -> {}
                }

                oldState = state
                state = State.ServerSideCode
                p++
                return true
            }

            if (next == null || !(next == '/' || next == '!' || next == '?' || next.isLetter())) {
                return false
            }

            // End the current node.
            if (state == State.Text) {
                appendText(p)
            } else {
                finishTag(close = true, end = p, interrupted = true)
            }

            if (next == '!' || next == '?') {
                fullComment = p + 3 < text.length && text[p + 2] == '-' && text[p + 3] == '-'
                state = State.Comment
                p++
                return true
            }

            beginTag(at = p, start = next != '/')
            return true
        }

        /** Runs when a start tag's name is complete: the new tag may close its open parent(s). */
        private fun endTagName() {
            if (isStartTag) {
                val name = lower(text.substring(nameStart, p))

                while (top !== document && startClosesParent(top.name, name)) {
                    stack.removeLast()
                }
            }

            nameEnd = p
        }

        /** The tag ended at [end] (exclusive): '>' when not [interrupted], otherwise a new '<'. */
        private fun finishTag(close: Boolean, end: Int, interrupted: Boolean = false) {
            val name = tagName()

            if (!isStartTag) {
                closeEndTag(name, end)

                if (!interrupted) {
                    beginText(end)
                }

                return
            }

            val element = HapElement(
                name,
                attributes.map { a ->
                    HapAttribute(
                        if (a.nameEnd < 0) "" else lower(text.substring(a.nameStart, a.nameEnd)),
                        if (a.valueStart < 0 || a.valueEnd < 0) "" else text.substring(a.valueStart, a.valueEnd),
                    )
                },
            )
            top.append(element)
            stack.addLast(element)

            if (isCDataElement(name)) {
                // Left open; a following tag (when interrupted) nests inside it.
                if (!interrupted) {
                    state = State.PcData
                    cdataStart = end
                }

                return
            }

            if (close || isEmptyElement(name)) {
                stack.removeLast()
            }

            if (!interrupted) {
                beginText(end)
            }
        }

        private fun closeEndTag(name: String, end: Int) {
            val index = stack.indexOfLast { it !== document && it.name == name }

            when {
                index >= 0 -> while (stack.size > index) stack.removeLast()
                // A stray </br> is read as <br>.
                name == "br" -> top.append(HapElement("br"))
                // A stray </form> becomes (lower-cased) text.
                name == "form" -> top.append(HapText(lower(text.substring(tagStart, end))))
                else -> {}
            }
        }
    }
}
