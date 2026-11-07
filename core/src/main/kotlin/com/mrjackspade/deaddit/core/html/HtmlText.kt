package com.mrjackspade.deaddit.core.html

import org.jsoup.parser.Parser

/**
 * Text Reddit sends HTML-escaped and Reddit.Api passes on as it is (a post's title, a message's
 * subject: "&amp;" for "&"). The C# app wrote it into its WebView's HTML, so the browser showed it
 * decoded; this is that decoding, for display only. Everything else (blocklist matching, sharing,
 * copying) uses the text as Reddit sent it, as the C# app did.
 */
object HtmlText {
    fun display(text: String): String = Parser.unescapeEntities(text, false)
}
