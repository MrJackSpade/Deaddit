package com.mrjackspade.deaddit.core.html

import org.junit.Assert.assertEquals
import org.junit.Test

class RedditHtmlTest {
    @Test
    fun parsesApiHtml() {
        val html = "<!-- SC_OFF --><div class=\"md\"><p>Hello <strong>world</strong> &amp; &lt;3</p>\n</div><!-- SC_ON -->"
        assertEquals(
            listOf(Block.Paragraph(listOf(Span("Hello "), Span("world", bold = true), Span(" & <3")))),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun keepsCodeBlockLines() {
        val html = "<div class=\"md\"><pre><code>line one\n  line two\n</code></pre>\n</div>"
        assertEquals(listOf(Block.Code("line one\n  line two")), RedditHtml.parse(html))
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

    // www.reddit.com's rendering of comment bodies (ShredditCommentParser's body HTML).

    @Test
    fun parsesWwwBodies() {
        val html = """<div class="py-0 xs:mx-xs" dir="auto" id="t1_x-post-rtjson-content"> <p> It ends, <shreddit-spoiler>Cirno wins</shreddit-spoiler>, and <s>not</s> sadly. </p> </div>"""
        assertEquals(
            listOf(Block.Paragraph(listOf(Span("It ends, "), Span("Cirno wins", spoiler = 0), Span(", and "), Span("not", strike = true), Span(" sadly.")))),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun readsWwwWrappersAsTheirContent() {
        // A bot's comment: www wraps it in <shreddit-mod-comment> and a span around the body's div.
        val html = """<shreddit-mod-comment> <span class=""><div dir="auto" id="t1_x-post-rtjson-content"> <p> Hello </p><ul> <li> <p> one </p> </li><li> <p> two </p> </li> </ul></div></span></shreddit-mod-comment>"""
        assertEquals(
            listOf(
                Block.Paragraph(listOf(Span("Hello"))),
                Block.BulletList(ordered = false, start = 1, items = listOf(listOf(Block.Paragraph(listOf(Span("one")))), listOf(Block.Paragraph(listOf(Span("two")))))),
            ),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun readsWwwImagesAsTheirLink() {
        // As the markdown had them: a paragraph that is just the link, with its URL as its text.
        val url = "https://preview.redd.it/gas-station-v0-dtzhj31gra0i1.jpeg?width=4284&format=pjpg"
        val html = """<div id="t1_x-post-rtjson-content"> <figure class="rte-media"> <div class="w-fit"> <a href="${url.replace("&", "&amp;")}"> <img alt="Comment Image" src="x"> </a> </div> </figure> <p> Burger and beer. </p> </div>"""
        assertEquals(
            listOf(Block.Paragraph(listOf(Span(url, link = url))), Block.Paragraph(listOf(Span("Burger and beer.")))),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun readsAWwwGifInsideAParagraph() {
        // A GIF in the middle of a paragraph, wrapped in a loader.
        val gif = "https://external-preview.redd.it/5ySy6aW.gif?auto=webp"
        val html = """<shreddit-async-loader bundlename="shreddit_player_loader"> <div id="t1_x-post-rtjson-content"> <p> Yeah... flour??<br/> <figure class="rte-media"> <div> <a href="$gif"><div><shreddit-player gif="" src="y"><source src="y"></shreddit-player></div></a></div></figure></p></div></shreddit-async-loader>"""
        assertEquals(
            listOf(Block.Paragraph(listOf(Span("Yeah... flour??")))  , Block.Paragraph(listOf(Span(gif, link = gif)))),
            RedditHtml.parse(html),
        )
    }
}
