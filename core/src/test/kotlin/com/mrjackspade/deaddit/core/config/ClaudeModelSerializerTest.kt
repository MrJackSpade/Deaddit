package com.mrjackspade.deaddit.core.config

import org.junit.Assert.assertEquals
import org.junit.Test

/** Expected values are what the C# ClaudeModelJsonConverter read on .NET 10. */
class ClaudeModelSerializerTest {
    private fun read(json: String): ClaudeModel =
        SettingsJson.decodeFromString(AiPrompt.serializer(), """{"Model":$json}""").model

    private fun string(value: String) = read(kotlinx.serialization.json.JsonPrimitive(value).toString())

    @Test
    fun readsNamesAsEnumTryParse() {
        assertEquals(ClaudeModel.Claude_Opus_4_8, string("claude_opus_4_8"))
        assertEquals(ClaudeModel.Claude_Haiku_4_5, string(" Claude_Haiku_4_5 "))
        assertEquals(ClaudeModel.None, string("None"))
        assertEquals(ClaudeModel.None, string("x"))
        assertEquals(ClaudeModel.None, string(""))
        assertEquals(ClaudeModel.None, string(" "))
        assertEquals(ClaudeModel.None, string("Claude_Opus_4_8,"))
        assertEquals(ClaudeModel.None, string("Claude_Opus_4_K8"))
        // Names separated by commas are OR'd: 1 | 2 is 3.
        assertEquals(ClaudeModel.Claude_Opus_4_6, string("Claude_Opus_4_8, Claude_Opus_4_7"))
    }

    @Test
    fun readsNumbersAsEnumTryParse() {
        assertEquals(ClaudeModel.Claude_Opus_4_6, string("3"))
        assertEquals(ClaudeModel.Claude_Haiku_4_5, string("8"))
        assertEquals(ClaudeModel.Claude_Opus_4_7, string("+2 "))
        assertEquals(ClaudeModel.Claude_Opus_4_7, string("2\t"))
        assertEquals(ClaudeModel.Claude_Opus_4_8, string(" 1"))
        assertEquals(ClaudeModel.Claude_Opus_4_6, string("3 \u0000"))
        assertEquals(ClaudeModel.None, string("9"))
        assertEquals(ClaudeModel.None, string("-1"))
        assertEquals(ClaudeModel.None, string("1.0"))
        assertEquals(ClaudeModel.None, string("2147483648"))
        assertEquals(ClaudeModel.None, string("0x1"))
        assertEquals(ClaudeModel.None, string("- 1"))
    }

    @Test
    fun nonStringsAreNone() {
        assertEquals(ClaudeModel.None, read("null"))
        assertEquals(ClaudeModel.None, read("3"))
        assertEquals(ClaudeModel.None, read("true"))
        assertEquals(ClaudeModel.None, read("{}"))
    }

    @Test
    fun missingIsTheDefault() {
        assertEquals(ClaudeModel.Claude_Sonnet_4_5, SettingsJson.decodeFromString(AiPrompt.serializer(), "{}").model)
    }
}
