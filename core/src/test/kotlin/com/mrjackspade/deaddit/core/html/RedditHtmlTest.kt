package com.mrjackspade.deaddit.core.html

import org.junit.Assert.assertEquals
import org.junit.Test

class RedditHtmlTest {
    @Test
    fun parsesEscapedApiHtml() {
        val html = "&lt;div class=\"md\"&gt;&lt;p&gt;Hello &lt;strong&gt;world&lt;/strong&gt;&lt;/p&gt;\n&lt;/div&gt;"
        assertEquals(
            listOf(Block.Paragraph(listOf(Span("Hello "), Span("world", bold = true)))),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun parsesLinksAndInlineStyles() {
        val html = """<div class="md"><p>See <a href="https://example.com">the <em>site</em></a> and <code>x  y</code>, <del>no</del></p></div>"""
        assertEquals(
            listOf(
                Block.Paragraph(
                    listOf(
                        Span("See "),
                        Span("the ", link = "https://example.com"),
                        Span("site", italic = true, link = "https://example.com"),
                        Span(" and "),
                        Span("x  y", code = true),
                        Span(", "),
                        Span("no", strike = true),
                    ),
                ),
            ),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun parsesNestedQuotes() {
        val html = """<div class="md"><blockquote>
<p>outer</p>
<blockquote>
<p>inner</p>
</blockquote>
</blockquote>
<p>reply</p>
</div>"""
        assertEquals(
            listOf(
                Block.Quote(
                    listOf(
                        Block.Paragraph(listOf(Span("outer"))),
                        Block.Quote(listOf(Block.Paragraph(listOf(Span("inner"))))),
                    ),
                ),
                Block.Paragraph(listOf(Span("reply"))),
            ),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun parsesListsWithLooseAndNestedItems() {
        val html = """<div class="md"><ol start="3">
<li>three</li>
<li><p>four</p>
<ul>
<li>sub</li>
</ul></li>
</ol>
</div>"""
        assertEquals(
            listOf(
                Block.BulletList(
                    ordered = true,
                    start = 3,
                    items = listOf(
                        listOf(Block.Paragraph(listOf(Span("three")))),
                        listOf(
                            Block.Paragraph(listOf(Span("four"))),
                            Block.BulletList(false, 1, listOf(listOf(Block.Paragraph(listOf(Span("sub")))))),
                        ),
                    ),
                ),
            ),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun keepsCodeBlockWhitespace() {
        val html = "<div class=\"md\"><pre><code>a\n  b\n</code></pre>\n</div>"
        assertEquals(listOf(Block.Code("a\n  b")), RedditHtml.parse(html))
    }

    @Test
    fun parsesTables() {
        val html = """<div class="md"><table><thead>
<tr>
<th align="left">A</th>
<th align="center">B</th>
</tr>
</thead><tbody>
<tr>
<td align="left">1</td>
<td align="center"><strong>2</strong></td>
</tr>
</tbody></table>
</div>"""
        assertEquals(
            listOf(
                Block.Table(
                    header = listOf(Cell(Align.Start, listOf(Span("A"))), Cell(Align.Center, listOf(Span("B")))),
                    rows = listOf(listOf(Cell(Align.Start, listOf(Span("1"))), Cell(Align.Center, listOf(Span("2", bold = true))))),
                ),
            ),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun tableCellsWithoutAlignmentAreUnspecified() {
        val html = """<div class="md"><table><thead><tr><th>A</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table></div>"""
        assertEquals(
            listOf(
                Block.Table(
                    header = listOf(Cell(Align.Unspecified, listOf(Span("A")))),
                    rows = listOf(listOf(Cell(Align.Unspecified, listOf(Span("1"))))),
                ),
            ),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun numbersSpoilersAndSuperscripts() {
        val html = """<div class="md"><p><span class="md-spoiler-text">one</span> x<sup>2<sup>3</sup></sup> <span class="md-spoiler-text">two</span></p></div>"""
        assertEquals(
            listOf(
                Block.Paragraph(
                    listOf(
                        Span("one", spoiler = 0),
                        Span(" x"),
                        Span("2", superscript = 1),
                        Span("3", superscript = 2),
                        Span(" "),
                        Span("two", spoiler = 1),
                    ),
                ),
            ),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun keepsLineBreaksAndHeadingsAndRules() {
        val html = """<div class="md"><h2>Title</h2>
<p>line one<br/>
line two</p>
<hr/>
</div>"""
        assertEquals(
            listOf(
                Block.Heading(2, listOf(Span("Title"))),
                Block.Paragraph(listOf(Span("line one\nline two"))),
                Block.Rule,
            ),
            RedditHtml.parse(html),
        )
    }
}
