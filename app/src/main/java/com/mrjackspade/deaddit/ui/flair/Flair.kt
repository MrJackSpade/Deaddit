package com.mrjackspade.deaddit.ui.flair

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.text.InlineTextContent
import androidx.compose.foundation.text.appendInlineContent
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.Placeholder
import androidx.compose.ui.text.PlaceholderVerticalAlign
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import coil3.compose.AsyncImage
import com.mrjackspade.deaddit.core.model.FlairPart
import com.mrjackspade.deaddit.core.util.CssColor
import com.mrjackspade.deaddit.core.util.CssColorValue
import com.mrjackspade.deaddit.ui.LocalHacks
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.chipShape
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.html.InlineFlow
import com.mrjackspade.deaddit.ui.html.inlineBackground
import com.mrjackspade.deaddit.ui.px

private val EMOJI_TAG = Regex(":[a-zA-Z0-9\\-_]*:")

/**
 * A user or link flair: its richtext (with emoji images) when there is some and flair images are
 * resolved, otherwise its text. Draws nothing when there's nothing to show.
 * [textColor] is Reddit's "dark"/"light"; [background] is a CSS color or null.
 *
 * [inline] is how a comment shows it, on a line of its own: text flair is an inline box there, its
 * padding painted over the lines around it without making its line taller ([InlineChip]). A post
 * shows it as a block (a flex item in the C# app), padding and margin included in its height.
 * Richtext flair is an inline-flex box either way, so its padding always counts.
 */
@Composable
fun Flair(text: String?, richtext: List<FlairPart>, background: String?, textColor: String?, modifier: Modifier = Modifier, inline: Boolean = false) {
    val styling = LocalStyling.current
    val hacks = LocalHacks.current

    val resolveImages = !hacks.skipResolvingFlairImages
    val parts = if (resolveImages && richtext.isNotEmpty()) richtext.takeIf { it.any(::isShown) }?.let(::inlineParts).orEmpty() else null
    val plain = if (parts == null) text?.let { if (hacks.skipResolvingFlairImages) it.replace(EMOJI_TAG, "").trim() else it } else null

    if (parts.isNullOrEmpty() && plain.isNullOrBlank()) return

    val flairText = when (textColor) {
        "dark" -> styling.flairDarkTextColor.color
        "light" -> styling.flairLightTextColor.color
        else -> styling.subTextColor.color
    }

    // The C# app put the color into the flair's style, where a value the browser rejects applied
    // nothing: no background, or (swapped) the inherited text color.
    val parsed = remember(background) { background?.let(CssColor::parse) }
    val swapped = styling.swapFlairColors && background != null

    // Muted (the default), every flair is a quiet chip in the app's colors, whatever the subreddit's.
    val fg = when {
        styling.mutedFlair -> styling.subTextColor.color
        swapped -> (parsed as? CssColorValue.Argb)?.let { Color(it.argb) } ?: Color.Unspecified
        else -> flairText
    }
    val bg = when {
        styling.mutedFlair -> styling.tertiaryColor.color
        swapped || background == null -> styling.primaryColor.color
        parsed is CssColorValue.Argb -> Color(parsed.argb)
        parsed == CssColorValue.CurrentColor -> fg
        else -> Color.Transparent
    }
    val fontSize = (styling.subTextFontSize * 0.9).px

    if (parts == null && inline) {
        InlineFlow(modifier) { InlineChip(plain!!, fg, bg, fontSize) }
        return
    }

    val box: @Composable (Modifier) -> Unit = { m ->
        Row(
            m.background(bg, styling.chipShape).padding(horizontal = 7.dp, vertical = 1.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            if (parts != null) {
                RichtextLine(parts, fg, fontSize, (styling.subTextFontSize * 1.1).toInt().dp)
            } else {
                Text(plain!!, color = fg, fontSize = fontSize)
            }
        }
    }

    if (inline) InlineFlow(modifier) { box(Modifier) } else box(modifier)
}

/**
 * A text flair or user tag as an inline box (CSS display: inline): 4px of padding and 2px of
 * margin across, the padding above and below painted only, with 4px corners.
 */
@Composable
fun InlineChip(text: String, color: Color, background: Color, fontSize: TextUnit) {
    Text(
        text,
        Modifier.inlineBackground(background, padding = 4.dp, radius = 4.dp, marginStart = 2.dp, marginEnd = 2.dp),
        color = color,
        fontSize = fontSize,
    )
}

/**
 * Richtext flair as one line of text, as www lays it out inline: emoji images side by side with
 * nothing between them (a flair can be one picture cut into several), text with its own spaces.
 */
@Composable
private fun RichtextLine(parts: List<FlairPart>, color: Color, fontSize: TextUnit, emojiSize: Dp) {
    val emojiSp = with(LocalDensity.current) { emojiSize.toSp() }
    val line = remember(parts) {
        buildAnnotatedString {
            parts.forEachIndexed { i, part -> if (part.type == "emoji") appendInlineContent("$i", part.alias.orEmpty()) else append(part.text!!) }
        }
    }
    val images = remember(parts, emojiSp) {
        parts.withIndex().filter { it.value.type == "emoji" }.associate { (i, part) ->
            "$i" to InlineTextContent(Placeholder(emojiSp, emojiSp, PlaceholderVerticalAlign.Center)) {
                AsyncImage(part.url, contentDescription = part.alias, modifier = Modifier.fillMaxSize())
            }
        }
    }
    Text(line, color = color, fontSize = fontSize, inlineContent = images)
}

/**
 * The parts drawn: images with a link and text that isn't empty, whitespace kept between them
 * but not at either end, where a browser drops it.
 */
private fun inlineParts(richtext: List<FlairPart>): List<FlairPart> {
    val parts = richtext.filter { if (it.type == "emoji") !it.url.isNullOrBlank() else !it.text.isNullOrEmpty() }.toMutableList()
    while (parts.firstOrNull()?.let { it.type != "emoji" } == true) {
        val text = parts.first().text!!.trimStart()
        if (text.isEmpty()) parts.removeAt(0) else { parts[0] = parts[0].copy(text = text); break }
    }
    while (parts.lastOrNull()?.let { it.type != "emoji" } == true) {
        val text = parts.last().text!!.trimEnd()
        if (text.isEmpty()) parts.removeAt(parts.lastIndex) else { parts[parts.lastIndex] = parts.last().copy(text = text); break }
    }
    return parts
}

private fun isShown(part: FlairPart): Boolean =
    if (part.type == "emoji") !part.url.isNullOrBlank() else !part.text.isNullOrBlank()
