package com.mrjackspade.deaddit.core.config

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class LanternTest {
    @Test
    fun theDefaultsAreBalancedFaintDefaultAmber() {
        val styling = ApplicationStyling()
        assertEquals(Lantern.Contrast.Balanced, Lantern.contrastOf(styling))
        assertEquals(Lantern.Edges.Faint, Lantern.edgesOf(styling))
        assertEquals(Lantern.TextSize.Default, Lantern.textSizeOf(styling))
        assertEquals("Amber", Lantern.accentOf(styling)?.name)
    }

    @Test
    fun applyingTheDefaultPresetsChangesNothing() {
        var styling = ApplicationStyling()
        styling = Lantern.withContrast(styling, Lantern.Contrast.Balanced)
        styling = Lantern.withEdges(styling, Lantern.Edges.Faint)
        styling = Lantern.withTextSize(styling, Lantern.TextSize.Default)
        styling = Lantern.withAccent(styling, Lantern.accents.first())
        assertEquals(ApplicationStyling(), styling)
    }

    @Test
    fun eachContrastLevelIsToldBackAndKeepsTheEdges() {
        for (contrast in Lantern.Contrast.entries) {
            val clear = Lantern.withContrast(Lantern.withEdges(ApplicationStyling(), Lantern.Edges.Clear), contrast)
            assertEquals(contrast, Lantern.contrastOf(clear))
            assertEquals(Lantern.Edges.Clear, Lantern.edgesOf(clear))
        }
    }

    @Test
    fun aHandEditedColorMatchesNoContrastLevel() {
        val styling = ApplicationStyling(cardColor = DynamicColor.parse("#123456"))
        assertNull(Lantern.contrastOf(styling))
        assertNull(Lantern.edgesOf(styling))
    }

    @Test
    fun theAccentMarksUpvotesAndTheOp() {
        val sky = Lantern.accents.first { it.name == "Sky" }
        val styling = Lantern.withAccent(ApplicationStyling(), sky)
        assertEquals(sky.color, styling.accentColor)
        assertEquals(sky.color, styling.upvoteColor)
        assertEquals(sky.color, styling.opTextColor)
        assertEquals("Sky", Lantern.accentOf(styling)?.name)
    }

    @Test
    fun aStylingSavedBeforeTheNewFieldsReadsThemAsDefaults() {
        val styling = SettingsJson.decodeFromString(ApplicationStyling.serializer(), """{ "TitleFontSize": 16 }""")
        assertEquals(ApplicationStyling().cardColor, styling.cardColor)
        assertEquals(10, styling.cornerRadius)
        assertEquals(16.0, styling.titleFontSize, 0.0)
    }
}
