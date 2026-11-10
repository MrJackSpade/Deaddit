package com.mrjackspade.deaddit.core.config

import org.junit.Assert.assertEquals
import org.junit.Test

class SettingsImportTest {
    /** The shape System.Text.Json gave the C# app's export (PascalCase, enums by name). */
    private val export = """
        {
          "ExportVersion": "1.0",
          "Styling": { "PrimaryColor": { "Alpha": 255, "Blue": 33, "Green": 33, "Red": 33 }, "TitleFontSize": 18, "VisitedOpacity": 0.3 },
          "AIConfiguration": { "ApiKey": "k", "Prompts": [ { "DisplayName": "Fix", "Model": "Claude_Opus_4_1", "TextContent": "{0}" }, { "DisplayName": "Old", "Model": "Claude_3_Opus", "TextContent": "" } ] },
          "BlockConfiguration": {
            "BlackList": { "Rules": [ { "Author": "spam", "BlockType": "Both", "RuleName": "/u/spam", "IsNsfw": false } ] },
            "WhiteList": { "Rules": [] },
            "DuplicateTitleHandling": "Visited",
            "MinAccountAgeDays": 3
          },
          "ApplicationHacks": { "PageSize": 50, "DefaultRegion": "US_WA", "TitleEmojiHandling": "Strip", "SomethingRemoved": true },
          "SavePaths": { "DefaultImageDirectory": null },
          "LandingPageConfiguration": { "Subscriptions": [ { "DisplayString": "r/pics", "Sort": "Hot", "SubReddit": "r/pics" } ] },
          "PostHistory": [ "t3_a", "t3_b" ],
          "BearerTokenJson": null,
          "UserTags": { "someone": "nice" }
        }
    """.trimIndent()

    @Test
    fun importsTheCSharpExport() {
        val data = SettingsJson.decodeFromString(SettingsExportData.serializer(), export)

        assertEquals(DynamicColor(255, 33, 33, 33), data.styling.primaryColor)
        assertEquals(18.0, data.styling.titleFontSize, 0.0)
        assertEquals(ApplicationStyling().upvoteColor, data.styling.upvoteColor)
        assertEquals(ClaudeModel.Claude_Opus_4_1, data.aiConfiguration.prompts[0].model)
        assertEquals(ClaudeModel.None, data.aiConfiguration.prompts[1].model)
        assertEquals("spam", data.blockConfiguration.blackList.rules.single().author)
        assertEquals(PostState.Visited, data.blockConfiguration.duplicateTitleHandling)
        assertEquals(3, data.blockConfiguration.minAccountAgeDays)
        assertEquals(50, data.applicationHacks.pageSize)
        assertEquals(Region.US_WA, data.applicationHacks.defaultRegion)
        assertEquals(OptionalStrip.Strip, data.applicationHacks.titleEmojiHandling)
        assertEquals("r/pics", data.landingPageConfiguration.subscriptions.single().thingName)
        assertEquals(listOf("t3_a", "t3_b"), data.postHistory)
        assertEquals(mapOf("someone" to "nice"), data.userTags)
    }

    /** The C# app's BearerToken record, as System.Text.Json wrote it into "BearerTokenJson". */
    @Test
    fun importsTheCSharpSession() {
        val utc = """{"AccessToken":"tok","AuthenticatedUser":"me","Expiration":"2026-11-07T01:02:03.4560000Z","SessionCookies":"a=b; c=d"}"""
        val session = SettingsJson.decodeFromString(ExportedSession.serializer(), utc).toSession()
        assertEquals("tok", session.accessToken)
        assertEquals("me", session.authenticatedUser)
        assertEquals("2026-11-07T01:02:03.456Z", session.expiration)
        assertEquals("a=b; c=d", session.sessionCookies)

        // A DateTime of unspecified kind: the C# expiry check treated it as UTC.
        val unspecified = """{"AccessToken":"tok","AuthenticatedUser":"me","Expiration":"2026-11-07T01:02:03","SessionCookies":null}"""
        assertEquals("2026-11-07T01:02:03Z", SettingsJson.decodeFromString(ExportedSession.serializer(), unspecified).toSession().expiration)

        val offset = """{"AccessToken":"tok","AuthenticatedUser":"me","Expiration":"2026-11-07T01:02:03-05:00"}"""
        assertEquals("2026-11-07T06:02:03Z", SettingsJson.decodeFromString(ExportedSession.serializer(), offset).toSession().expiration)

        val none = """{"AccessToken":"tok","AuthenticatedUser":"me","Expiration":null,"SessionCookies":null}"""
        assertEquals(null, SettingsJson.decodeFromString(ExportedSession.serializer(), none).toSession().expiration)
    }

    /** What this app exports reads back the same, in the C# shape. */
    @Test
    fun exportRoundTrips() {
        val data = SettingsJson.decodeFromString(SettingsExportData.serializer(), export)
        val written = SettingsJson.encodeToString(SettingsExportData.serializer(), data)
        assertEquals(data, SettingsJson.decodeFromString(SettingsExportData.serializer(), written))
        assert(written.contains("\"Model\":\"Claude_Opus_4_1\""))
        assert(written.contains("\"DefaultRegion\":\"US_WA\""))
        assert(written.contains("\"UserTags\":{\"someone\":\"nice\"}"))
    }

    @Test
    fun parsesHexColorsBothWays() {
        assertEquals(DynamicColor(255, 0, 69, 255), DynamicColor.parse("#ff4500"))
        assertEquals(DynamicColor(0x80, 0, 69, 255), DynamicColor.parse("#80ff4500"))
        assertEquals("#FF4500", DynamicColor.parse("#ff4500").toHex())
        assertEquals("#80FF4500", DynamicColor.parse("#80ff4500").toArgbHex())
    }
}
