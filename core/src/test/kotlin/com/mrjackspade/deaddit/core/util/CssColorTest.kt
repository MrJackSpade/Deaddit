package com.mrjackspade.deaddit.core.util

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertThrows
import org.junit.Assert.assertTrue
import org.junit.Test
import kotlin.math.abs

class CssColorTest {
    private fun argb(value: String): Int = (CssColor.parse(value) as CssColorValue.Argb).argb

    private fun assertColor(expected: Long, value: String) = assertEquals(value, expected.toInt(), argb(value))

    /** Within one step per channel: converting from Lab and OKLab rounds. */
    private fun assertNear(expected: Long, value: String) {
        val actual = argb(value)
        for (shift in listOf(24, 16, 8, 0)) {
            val e = (expected.toInt() shr shift) and 0xFF
            val a = (actual shr shift) and 0xFF
            assertTrue("$value: ${Integer.toHexString(actual)}", abs(e - a) <= 1)
        }
    }

    @Test
    fun readsHexAndKeywords() {
        assertColor(0xFFFF4500, "#ff4500")
        assertColor(0xFFFF4500, "  #FF4500 ")
        assertColor(0xFF112233, "#123")
        assertColor(0x44112233, "#1234")
        assertColor(0x80FF4500, "#ff450080")
        assertColor(0xFFFF0000, "Red")
        assertColor(0xFF663399, "rebeccapurple")
        assertColor(0x00000000, "transparent")
        assertColor(0xFFFF0000, "red !important")
        assertColor(0xFFFF0000, "red; color: blue")
        assertEquals(CssColorValue.CurrentColor, CssColor.parse("currentColor"))
    }

    @Test
    fun readsRgb() {
        assertColor(0xFFFF8000, "rgb(255, 128, 0)")
        assertColor(0xFFFF0000, "rgb(300, -5, 0)")
        assertColor(0xFF80FF00, "rgb(50%, 100%, 0%)")
        assertColor(0x80000000, "rgba(0,0,0,.5)")
        assertColor(0x80000000, "rgba(0, 0, 0, 50%)")
        assertColor(0x80FF0000, "rgb(255 0 0 / 50%)")
        assertColor(0xFF1A141E, "rgb(10% 20 30)")
        assertColor(0xFF00FF00, "rgb(none 255 none)")
        assertColor(0xFF000000, "RGB(0,0,0")
        assertColor(0xFF0A141E, "rgb(1e1, 2e1, 3e1)")
    }

    @Test
    fun readsHslAndHwb() {
        assertColor(0xFF00FF00, "hsl(120, 100%, 50%)")
        assertColor(0xFF800000, "hsl(0 100% 25%)")
        assertColor(0xFF0000FF, "hsla(0.6666667turn, 100%, 50%, 1)")
        assertColor(0xFF00FFFF, "hsl(180deg 100 50)")
        assertColor(0x80FF0000, "hsl(-360 100% 50% / .5)")
        assertColor(0xFFFF0000, "hwb(0 0% 0%)")
        assertColor(0xFF808080, "hwb(0 60% 60%)")
    }

    @Test
    fun readsLabFamilies() {
        assertNear(0xFFFF0000, "lab(54.29 80.82 69.88)")
        assertNear(0xFFFF0000, "lch(54.29 106.84 40.85)")
        assertNear(0xFFFF0000, "oklab(62.8% 0.2249 0.1258)")
        assertNear(0xFFFF0000, "oklch(0.628 0.2577 29.23deg)")
        assertNear(0xFFFFFFFF, "lab(100 0 0)")
        assertNear(0xFF000000, "oklch(0 0 0)")
    }

    @Test
    fun valuesTheBrowserIgnoresAreNull() {
        for (value in listOf("", " ", "notacolor", "#12", "#12345", "#ggg", "rgb(1, 2)", "rgb(10%, 20, 30)", "rgb(1, 2, 3, 4, 5)",
            "rgb(1 2 3 4)", "rgb(1, 2 3)", "rgb(1deg, 2, 3)", "hsl(0, 100, 50%)", "hsl(0, none, 50%)", "hwb(0, 0%, 0%)",
            "lab(1, 2, 3)", "nope(1 2 3)", "rgb(1 2 3) red", "red blue", "red !importan", "rgb(1 2 3 /)", "rgb(1 2 3 / 4 5)")) {
            assertNull(value, CssColor.parse(value))
        }
    }

    @Test
    fun validFormsNotImplementedThrow() {
        for (value in listOf("color(srgb 1 0 0)", "color-mix(in srgb, red, blue)", "light-dark(red, blue)", "rgb(from red r g b)",
            "rgb(calc(255) 0 0)", "Canvas", "inherit", "-webkit-link")) {
            assertThrows(value, UnsupportedOperationException::class.java) { CssColor.parse(value) }
        }
    }
}
