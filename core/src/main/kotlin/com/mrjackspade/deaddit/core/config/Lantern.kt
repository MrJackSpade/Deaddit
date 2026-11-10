package com.mrjackspade.deaddit.core.config

/**
 * The Lantern design's presets: what the Display page applies to [ApplicationStyling], and how it
 * tells which preset a styling is on. A styling changed by hand in Configuration may match none.
 */
object Lantern {
    /**
     * The defaults [ApplicationStyling] has now. A saved styling from an older version is replaced
     * by these defaults once, on the first launch that sees it (SettingsStore).
     */
    const val STYLING_VERSION = 2

    enum class Contrast { Soft, Balanced, Crisp }

    enum class Edges { Faint, Clear }

    /** Title, subtext and comment sizes. */
    enum class TextSize(val title: Double, val subText: Double, val comment: Double) {
        Default(17.0, 14.0, 15.0),
        Larger(19.0, 15.5, 17.0),
        Largest(21.0, 17.0, 19.0),
    }

    class Accent(val name: String, val color: DynamicColor)

    val accents = listOf(
        Accent("Amber", DynamicColor.parse("#F0A23A")),
        Accent("Sage", DynamicColor.parse("#5FC898")),
        Accent("Sky", DynamicColor.parse("#6FB0F2")),
        Accent("Mauve", DynamicColor.parse("#D58BCB")),
    )

    /** A contrast level's colors: bars and page, cards, a selected card, chips, both edge strengths, text. */
    private class Palette(
        val page: String,
        val card: String,
        val selected: String,
        val chip: String,
        val faintEdge: String,
        val clearEdge: String,
        val text: String,
        val subText: String,
    )

    private val palettes = mapOf(
        Contrast.Soft to Palette("#18191C", "#212327", "#292B30", "#2D2F35", "#34363C", "#474A51", "#E3E1DC", "#A09E98"),
        Contrast.Balanced to Palette("#141518", "#1F2125", "#282B30", "#2F3238", "#3A3D44", "#51555D", "#EFEDE8", "#AEACA6"),
        Contrast.Crisp to Palette("#101114", "#1D1F24", "#272A30", "#32353C", "#464A52", "#5E626B", "#F7F5F1", "#BDBBB5"),
    )

    private fun palette(contrast: Contrast) = palettes.getValue(contrast)

    /** The contrast level whose colors the styling has, or null when they've been changed by hand. */
    fun contrastOf(styling: ApplicationStyling): Contrast? = Contrast.entries.firstOrNull { contrast ->
        val p = palette(contrast)
        styling.primaryColor == c(p.page) && styling.secondaryColor == c(p.page) && styling.cardColor == c(p.card) &&
            styling.highlightColor == c(p.selected) && styling.tertiaryColor == c(p.chip) &&
            styling.textColor == c(p.text) && styling.subTextColor == c(p.subText) &&
            (styling.borderColor == c(p.faintEdge) || styling.borderColor == c(p.clearEdge))
    }

    /** The edge strength, for a styling on one of the contrast levels. */
    fun edgesOf(styling: ApplicationStyling): Edges? {
        val p = palette(contrastOf(styling) ?: return null)
        return if (styling.borderColor == c(p.clearEdge)) Edges.Clear else Edges.Faint
    }

    /** The contrast level's colors; the edges keep their strength (faint when it can't be told). */
    fun withContrast(styling: ApplicationStyling, contrast: Contrast): ApplicationStyling {
        val p = palette(contrast)
        val edges = edgesOf(styling) ?: Edges.Faint
        return styling.copy(
            primaryColor = c(p.page),
            secondaryColor = c(p.page),
            cardColor = c(p.card),
            highlightColor = c(p.selected),
            tertiaryColor = c(p.chip),
            borderColor = c(if (edges == Edges.Clear) p.clearEdge else p.faintEdge),
            textColor = c(p.text),
            subTextColor = c(p.subText),
            flairLightTextColor = c(p.text),
            flairDarkTextColor = c(p.page),
        )
    }

    /** The edge strength at the styling's contrast level (Balanced when it's on none). */
    fun withEdges(styling: ApplicationStyling, edges: Edges): ApplicationStyling {
        val contrast = contrastOf(styling)
        val base = if (contrast == null) withContrast(styling, Contrast.Balanced) else styling
        val p = palette(contrast ?: Contrast.Balanced)
        return base.copy(borderColor = c(if (edges == Edges.Clear) p.clearEdge else p.faintEdge))
    }

    fun textSizeOf(styling: ApplicationStyling): TextSize? = TextSize.entries.firstOrNull {
        styling.titleFontSize == it.title && styling.subTextFontSize == it.subText && styling.commentFontSize == it.comment
    }

    fun withTextSize(styling: ApplicationStyling, size: TextSize): ApplicationStyling =
        styling.copy(titleFontSize = size.title, subTextFontSize = size.subText, commentFontSize = size.comment)

    fun accentOf(styling: ApplicationStyling): Accent? = accents.firstOrNull { it.color == styling.accentColor }

    /** The accent marks what's active and selected, upvotes, and the OP. */
    fun withAccent(styling: ApplicationStyling, accent: Accent): ApplicationStyling = styling.copy(
        accentColor = accent.color,
        upvoteColor = accent.color,
        opTextColor = accent.color,
        opBackgroundColor = blend(accent.color, styling.cardColor, 0.25),
    )

    /** [over] at [amount] opacity on [under], opaque. */
    private fun blend(over: DynamicColor, under: DynamicColor, amount: Double): DynamicColor {
        fun mix(a: Int, b: Int) = Math.round(a * amount + b * (1 - amount)).toInt()
        return DynamicColor(255, mix(over.blue, under.blue), mix(over.green, under.green), mix(over.red, under.red))
    }

    private fun c(hex: String) = DynamicColor.parse(hex)
}
