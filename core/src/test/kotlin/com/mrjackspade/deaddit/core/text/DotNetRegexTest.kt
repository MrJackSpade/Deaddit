package com.mrjackspade.deaddit.core.text

import org.junit.Assert.assertFalse
import org.junit.Assert.assertThrows
import org.junit.Assert.assertTrue
import org.junit.Test

/** What .NET's Regex.IsMatch(input, pattern) gives for each, with default options. */
class DotNetRegexTest {
    @Test
    fun classesAreUnicode() {
        assertTrue(DotNetRegex.isMatch("٣٤", "^\\d+$"))
        assertTrue(DotNetRegex.isMatch("café", "^\\w+$"))
        assertTrue(DotNetRegex.isMatch("a b", "a\\sb"))
        assertFalse(DotNetRegex.isMatch("٣", "\\D"))
        assertFalse(DotNetRegex.isMatch("é", "\\W"))
        assertTrue(DotNetRegex.isMatch("x٣", "^[x\\d]+$"))
        assertFalse(DotNetRegex.isMatch("é", "[\\W]"))
    }

    @Test
    fun wordBoundariesAreUnicode() {
        assertFalse(DotNetRegex.isMatch("écat", "\\bcat"))
        assertTrue(DotNetRegex.isMatch("é cat", "\\bcat\\b"))
        assertTrue(DotNetRegex.isMatch("écat", "\\Bcat"))
        // In a class, \b is a backspace.
        assertTrue(DotNetRegex.isMatch("\b", "[\\b]"))
    }

    @Test
    fun onlyNewlineEndsALine() {
        assertTrue(DotNetRegex.isMatch("a\rb", "a.b"))
        assertFalse(DotNetRegex.isMatch("a\nb", "a.b"))
        assertTrue(DotNetRegex.isMatch("end\n", "end$"))
        assertFalse(DotNetRegex.isMatch("end\r", "end$"))
    }

    @Test
    fun isCaseSensitiveUnlessAsked() {
        assertFalse(DotNetRegex.isMatch("foo", "Foo"))
        assertTrue(DotNetRegex.isMatch("ÉCOLE", "(?i)école"))
    }

    @Test
    fun readsDotNetLiteralsAndSyntax() {
        assertTrue(DotNetRegex.isMatch("a{", "a{"))
        assertTrue(DotNetRegex.isMatch("{x}", "^{x}$"))
        assertTrue(DotNetRegex.isMatch("aaa", "^a{3}$"))
        assertTrue(DotNetRegex.isMatch("a[", "^[a[]+$"))
        assertTrue(DotNetRegex.isMatch("&&", "^[&]+$"))
        assertTrue(DotNetRegex.isMatch("]", "[]]"))
        assertTrue(DotNetRegex.isMatch("b", "^[a-z-[aeiou]]$"))
        assertFalse(DotNetRegex.isMatch("a", "^[a-z-[aeiou]]$"))
        assertTrue(DotNetRegex.isMatch("xy", "x(?#a comment)y"))
        assertTrue(DotNetRegex.isMatch("abab", "(?'pair'ab)\\k'pair'"))
        assertTrue(DotNetRegex.isMatch("a\u0000b", "a\\0b"))
        assertTrue(DotNetRegex.isMatch("\n", "\\012"))
        assertTrue(DotNetRegex.isMatch("α", "\\p{IsGreek}"))
    }

    @Test
    fun invalidPatternsThrow() {
        assertThrows(Exception::class.java) { DotNetRegex.isMatch("a", "(") }
        assertThrows(Exception::class.java) { DotNetRegex.isMatch("a", "[a") }
    }
}
