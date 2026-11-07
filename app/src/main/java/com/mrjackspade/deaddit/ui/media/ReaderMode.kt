package com.mrjackspade.deaddit.ui.media

import android.content.Context
import android.text.TextUtils
import android.util.Base64
import com.mrjackspade.deaddit.core.config.ApplicationStyling
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.jsonPrimitive
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.ZonedDateTime
import java.time.format.DateTimeFormatter
import java.time.format.DateTimeParseException
import java.time.temporal.TemporalAccessor
import java.util.Locale

/** The readable portion of a web page (the C# ReaderArticle). */
data class ReaderArticle(
    val title: String?,
    val author: String?,
    val siteName: String?,
    val publicationDate: LocalDate?,
    /** Whole minutes; 0 when the article has no text. */
    val timeToReadMinutes: Int,
    val content: String,
)

/**
 * Reader mode (the C# ReaderModeExtractor). The C# ran SmartReader, a .NET port of Mozilla's
 * Readability, over the page's live DOM; this runs Mozilla's Readability itself (bundled in
 * assets/readability) inside the page, over a clone of that same live DOM.
 */
object ReaderMode {
    /** What [script] returns for a page without a document to read. */
    sealed interface Result {
        data object Unreadable : Result
        data object NoArticle : Result
        class Article(val article: ReaderArticle) : Result
    }

    /**
     * JavaScript that runs Readability over the page and returns the article as base64'd JSON
     * (raw HTML doesn't survive the evaluateJavascript round trip), "null" for no article, or
     * null when the page has no document.
     */
    fun script(context: Context): String {
        val readability = context.assets.open("readability/Readability.js").use { it.readBytes().toString(Charsets.UTF_8) }
        return """
            (function () {
                if (!document.documentElement) return null;
                $readability
                var a = new Readability(document.cloneNode(true)).parse();
                var json = a ? JSON.stringify({
                    title: a.title, byline: a.byline, siteName: a.siteName, publishedTime: a.publishedTime,
                    lang: a.lang, content: a.content, textContent: a.textContent
                }) : "null";
                return btoa(unescape(encodeURIComponent(json)));
            })()
        """.trimIndent()
    }

    /** Reads the value evaluateJavascript gave back for [script]. */
    fun parse(evaluated: String?): Result {
        if (evaluated.isNullOrBlank() || evaluated == "null") return Result.Unreadable

        // Android hands the result back as a quoted JS string literal
        val base64 = Json.parseToJsonElement(evaluated).jsonPrimitive.content
        val json = Base64.decode(base64, Base64.DEFAULT).toString(Charsets.UTF_8)
        val element = Json.parseToJsonElement(json)
        if (element is JsonNull) return Result.NoArticle

        val o = element as JsonObject
        fun field(name: String) = o[name]?.takeIf { it !is JsonNull }?.jsonPrimitive?.contentOrNull

        val content = field("content")
        if (content.isNullOrBlank()) return Result.NoArticle

        return Result.Article(
            ReaderArticle(
                title = field("title"),
                author = field("byline"),
                siteName = field("siteName"),
                publicationDate = field("publishedTime")?.let(::parseDate),
                timeToReadMinutes = timeToRead(field("textContent") ?: "", field("lang")),
                content = content,
            ),
        )
    }

    /**
     * SmartReader's TimeToReadCalculator: letters (not spaces or punctuation) over the
     * language's reading speed in characters per minute, at least a minute when there's text.
     */
    private fun timeToRead(text: String, language: String?): Int {
        if (text.isEmpty()) return 0
        val letters = text.count { it != ' ' && !isPunctuation(it) }
        val minutes = letters / charactersPerMinute(language)
        return if (minutes > 0) minutes else 1
    }

    /** .NET's char.IsPunctuation: the Unicode punctuation categories. */
    private fun isPunctuation(c: Char) = when (Character.getType(c).toByte()) {
        Character.CONNECTOR_PUNCTUATION, Character.DASH_PUNCTUATION, Character.START_PUNCTUATION, Character.END_PUNCTUATION,
        Character.INITIAL_QUOTE_PUNCTUATION, Character.FINAL_QUOTE_PUNCTUATION, Character.OTHER_PUNCTUATION -> true
        else -> false
    }

    // based on http://iovs.arvojournals.org/article.aspx?articleid=2166061 (SmartReader's table, by language code)
    private val CHARACTERS_PER_MINUTE = mapOf(
        "ar" to 612, "zh" to 255, "nl" to 978, "en" to 987, "fi" to 1078, "fr" to 998, "de" to 920, "he" to 833, "iw" to 833,
        "it" to 950, "ja" to 357, "pl" to 916, "pt" to 913, "sv" to 917, "sl" to 885, "es" to 1025, "ru" to 986, "tr" to 1054,
    )

    /** The language's reading speed; 960 (the average excluding the three outlier languages) for others, as SmartReader. */
    private fun charactersPerMinute(language: String?): Int {
        val code = language?.split('-', '_')?.firstOrNull()?.lowercase(Locale.ROOT)
        return CHARACTERS_PER_MINUTE[code] ?: 960
    }

    /**
     * The publication date from Readability's publishedTime (from the page's metadata). A value
     * that isn't a date gives no date, as SmartReader's metadata parsing did (DateTime.TryParse).
     */
    private fun parseDate(value: String): LocalDate? {
        val parsers: List<(String) -> TemporalAccessor> = listOf(
            { OffsetDateTime.parse(it) }, { ZonedDateTime.parse(it) }, { LocalDateTime.parse(it) }, { LocalDate.parse(it) },
        )
        for (parser in parsers) {
            try {
                return LocalDate.from(parser(value.trim()))
            } catch (_: DateTimeParseException) {
                continue
            }
        }
        return null
    }

    /** The article in the C# reader template, in the user's colors and sizes. */
    fun render(context: Context, article: ReaderArticle, styling: ApplicationStyling): String {
        val byline = mutableListOf<String>()
        article.author?.takeIf { it.isNotBlank() }?.let(byline::add)
        article.siteName?.takeIf { it.isNotBlank() }?.let(byline::add)
        article.publicationDate?.let { byline.add(it.format(DateTimeFormatter.ofPattern("MMMM d, yyyy"))) }
        if (article.timeToReadMinutes > 0) byline.add("${maxOf(1, article.timeToReadMinutes)} min read")

        val template = context.assets.open("media/reader.html").use { it.readBytes().toString(Charsets.UTF_8) }
        return template
            .replace("%BACKGROUND_COLOR%", styling.primaryColor.toHex())
            .replace("%TEXT_COLOR%", styling.textColor.toHex())
            .replace("%SUBTEXT_COLOR%", styling.subTextColor.toHex())
            .replace("%LINK_COLOR%", styling.hyperlinkColor.toHex())
            .replace("%CODE_COLOR%", styling.secondaryColor.toHex())
            .replace("%BODY_FONT_SIZE%", dotNet(styling.titleFontSize))
            .replace("%TITLE_FONT_SIZE%", dotNet(styling.titleFontSize * 1.6))
            .replace("%SUBTEXT_FONT_SIZE%", dotNet(styling.subTextFontSize))
            .replace("%TITLE%", TextUtils.htmlEncode(article.title ?: ""))
            .replace("%BYLINE%", TextUtils.htmlEncode(byline.joinToString(" · ")))
            .replace("%CONTENT%", article.content)
    }

    /** A double as .NET's ToString() wrote it: no ".0" on whole numbers. */
    private fun dotNet(value: Double): String =
        if (value == Math.floor(value) && !value.isInfinite()) value.toLong().toString() else value.toString()
}
