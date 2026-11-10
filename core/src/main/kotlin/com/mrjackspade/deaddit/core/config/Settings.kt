package com.mrjackspade.deaddit.core.config

import kotlinx.serialization.KSerializer
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.descriptors.PrimitiveKind
import kotlinx.serialization.descriptors.PrimitiveSerialDescriptor
import kotlinx.serialization.encoding.Decoder
import kotlinx.serialization.encoding.Encoder
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonDecoder
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonPrimitive
import com.mrjackspade.deaddit.core.text.DotNetText
import com.mrjackspade.deaddit.core.markdown.DotNetText as DotNetString
import com.mrjackspade.deaddit.core.session.Session
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.ZoneOffset

/*
 * The app's settings. Property names and enum values match the C# app's JSON (PascalCase names,
 * enum member names), so its "Export Settings" file imports as-is.
 */

val SettingsJson = Json {
    ignoreUnknownKeys = true
    encodeDefaults = true
    prettyPrint = false
}

/** An RGBA color, stored as its channels. */
@Serializable
data class DynamicColor(
    @SerialName("Alpha") val alpha: Int,
    @SerialName("Blue") val blue: Int,
    @SerialName("Green") val green: Int,
    @SerialName("Red") val red: Int,
) {
    fun toHex(): String = "#%02X%02X%02X".format(red, green, blue)

    fun toArgbHex(): String = "#%02X%02X%02X%02X".format(alpha, red, green, blue)

    /** As an Android/Compose ARGB int. */
    fun toArgb(): Int = (alpha shl 24) or (red shl 16) or (green shl 8) or blue

    companion object {
        /** Parses "#RRGGBB" or "#AARRGGBB". */
        fun parse(value: String): DynamicColor {
            val hex = value.trim('#')
            return when (hex.length) {
                6 -> DynamicColor(255, hex.substring(4, 6).toInt(16), hex.substring(2, 4).toInt(16), hex.substring(0, 2).toInt(16))
                8 -> DynamicColor(hex.substring(0, 2).toInt(16), hex.substring(6, 8).toInt(16), hex.substring(4, 6).toInt(16), hex.substring(2, 4).toInt(16))
                else -> throw IllegalArgumentException("Invalid color string format. Must be either 6 or 8 characters long (excluding '#').")
            }
        }
    }
}

enum class OptionalStrip { None, Strip }

enum class PostState(val label: String) { None("None"), Block("Block"), Visited("Mark As Read") }

enum class BlockType { Both, Post, Comment }

/** Sorts for subreddits, multis and the front page. Undefined means none. */
enum class PostSort(val path: String?) { Undefined(null), Hot("hot"), New("new"), Rising("rising"), Top("top") }

enum class InboxSort(val path: String?) { Undefined(null), Inbox("inbox"), Unread("unread"), Sent("sent") }

enum class UserProfileSort(val path: String) { Overview("overview"), Submitted("submitted"), Comments("comments") }

@Serializable
data class ApplicationHacks(
    @SerialName("AutoLoadCommentImages") val autoLoadCommentImages: Boolean = true,
    @SerialName("ConvertGifsToMp4") val convertGifsToMp4: Boolean = true,
    @SerialName("CommentEmojiHandling") val commentEmojiHandling: OptionalStrip = OptionalStrip.None,
    @SerialName("DefaultRegion") val defaultRegion: Region = Region.GLOBAL,
    @SerialName("SkipResolvingFlairImages") val skipResolvingFlairImages: Boolean = false,
    @SerialName("MinimumCommentBodyLenth") val minimumCommentBodyLength: Int = 0,
    @SerialName("MinimumPostBodyLenth") val minimumPostBodyLength: Int = 0,
    @SerialName("PageBuffer") val pageBuffer: Int = 2,
    @SerialName("PageSize") val pageSize: Int = 25,
    @SerialName("TitleEmojiHandling") val titleEmojiHandling: OptionalStrip = OptionalStrip.None,
    @SerialName("HideSelfKarma") val hideSelfKarma: Boolean = false,
    @SerialName("TitleNewlines") val titleNewlines: OptionalStrip = OptionalStrip.None,
    @SerialName("KeepAlive") val keepAlive: Boolean = false,
    @SerialName("SwearObfuscation") val swearObfuscation: Boolean = false,
)

/**
 * Every color and size the app draws with. The defaults are the Lantern design's Balanced look
 * (see [Lantern]); [Lantern.STYLING_VERSION] says which defaults a saved styling was made from.
 *
 * Roles: primary is the top and bottom bars, secondary the page, card the cards on it, highlight a
 * selected card, tertiary chips and active tabs, border the cards' edges, accent what's active or
 * selected.
 */
@Serializable
data class ApplicationStyling(
    @SerialName("AdminAuthorBackgroundColor") val adminAuthorBackgroundColor: DynamicColor = DynamicColor.parse("#40262A"),
    @SerialName("AdminAuthorTextColor") val adminAuthorTextColor: DynamicColor = DynamicColor.parse("#F28B82"),
    @SerialName("AdminTitleTextColor") val adminTitleTextColor: DynamicColor = DynamicColor.parse("#F28B82"),
    @SerialName("BlockedBackgroundColor") val blockedBackgroundColor: DynamicColor = DynamicColor.parse("#2E2224"),
    @SerialName("CommentFontSize") val commentFontSize: Double = 15.0,
    @SerialName("DistinguishedTitleColor") val distinguishedTitleColor: DynamicColor = DynamicColor.parse("#5FC898"),
    @SerialName("DownvoteColor") val downvoteColor: DynamicColor = DynamicColor.parse("#6FB0F2"),
    @SerialName("FlairDarkTextColor") val flairDarkTextColor: DynamicColor = DynamicColor.parse("#141518"),
    @SerialName("FlairLightTextColor") val flairLightTextColor: DynamicColor = DynamicColor.parse("#EFEDE8"),
    @SerialName("SwapFlairColors") val swapFlairColors: Boolean = false,
    @SerialName("HighlightColor") val highlightColor: DynamicColor = DynamicColor.parse("#282B30"),
    @SerialName("HyperlinkColor") val hyperlinkColor: DynamicColor = DynamicColor.parse("#A9C4E4"),
    @SerialName("ModeratorAuthorBackgroundColor") val moderatorAuthorBackgroundColor: DynamicColor = DynamicColor.parse("#1E3A2D"),
    @SerialName("ModeratorAuthorTextColor") val moderatorAuthorTextColor: DynamicColor = DynamicColor.parse("#5FC898"),
    @SerialName("ModeratorTitleTextColor") val moderatorTitleTextColor: DynamicColor = DynamicColor.parse("#5FC898"),
    @SerialName("OpBackgroundColor") val opBackgroundColor: DynamicColor = DynamicColor.parse("#53412A"),
    @SerialName("OpTextColor") val opTextColor: DynamicColor = DynamicColor.parse("#F0A23A"),
    @SerialName("PrimaryColor") val primaryColor: DynamicColor = DynamicColor.parse("#141518"),
    @SerialName("SecondaryColor") val secondaryColor: DynamicColor = DynamicColor.parse("#141518"),
    @SerialName("SubTextColor") val subTextColor: DynamicColor = DynamicColor.parse("#AEACA6"),
    @SerialName("SubTextFontSize") val subTextFontSize: Double = 14.0,
    @SerialName("TertiaryColor") val tertiaryColor: DynamicColor = DynamicColor.parse("#2F3238"),
    @SerialName("TextColor") val textColor: DynamicColor = DynamicColor.parse("#EFEDE8"),
    @SerialName("ThumbnailSize") val thumbnailSize: Int = 64,
    @SerialName("TitleFontSize") val titleFontSize: Double = 17.0,
    @SerialName("UpvoteColor") val upvoteColor: DynamicColor = DynamicColor.parse("#F0A23A"),
    @SerialName("VisitedOpacity") val visitedOpacity: Double = 0.22,
    @SerialName("CardColor") val cardColor: DynamicColor = DynamicColor.parse("#1F2125"),
    @SerialName("BorderColor") val borderColor: DynamicColor = DynamicColor.parse("#3A3D44"),
    @SerialName("AccentColor") val accentColor: DynamicColor = DynamicColor.parse("#F0A23A"),
    @SerialName("CornerRadius") val cornerRadius: Int = 10,
    /** Flair as a quiet chip in the app's colors, instead of the subreddit's own colors. */
    @SerialName("MutedFlair") val mutedFlair: Boolean = true,
)

@Serializable
data class BlockRule(
    @SerialName("Author") val author: String? = null,
    @SerialName("BlockType") val blockType: BlockType = BlockType.Both,
    @SerialName("Body") val body: String? = null,
    @SerialName("Domain") val domain: String? = null,
    @SerialName("Flair") val flair: String? = null,
    @SerialName("IsArchived") val isArchived: Boolean = false,
    @SerialName("IsLocked") val isLocked: Boolean = false,
    @SerialName("IsNsfw") val isNsfw: Boolean = false,
    @SerialName("RuleName") val ruleName: String? = null,
    @SerialName("SubReddit") val subReddit: String? = null,
    @SerialName("Title") val title: String? = null,
) {
    override fun toString(): String = ruleName ?: ""
}

@Serializable
data class BlockList(@SerialName("Rules") val rules: List<BlockRule> = emptyList())

@Serializable
data class BlockConfiguration(
    @SerialName("BlackList") val blackList: BlockList = BlockList(),
    @SerialName("DuplicateLinkHandling") val duplicateLinkHandling: PostState = PostState.None,
    @SerialName("DuplicateThumbHandling") val duplicateThumbHandling: PostState = PostState.None,
    @SerialName("DuplicateTitleHandling") val duplicateTitleHandling: PostState = PostState.None,
    @SerialName("MaxLinkKarma") val maxLinkKarma: Int = 0,
    @SerialName("MaxLinkKarmaRatio") val maxLinkKarmaRatio: Double = 0.0,
    @SerialName("MinAccountAgeDays") val minAccountAgeDays: Int = 0,
    @SerialName("MinCommentKarma") val minCommentKarma: Int = 0,
    @SerialName("WhiteList") val whiteList: BlockList = BlockList(),
)

/** The Claude models a prompt can use. A stored name no longer listed reads as [None]. */
@Serializable(ClaudeModelSerializer::class)
enum class ClaudeModel(val id: String) {
    None(""),
    Claude_Opus_4_8("claude-opus-4-8"),
    Claude_Opus_4_7("claude-opus-4-7"),
    Claude_Opus_4_6("claude-opus-4-6"),
    Claude_Opus_4_5("claude-opus-4-5-20251101"),
    Claude_Opus_4_1("claude-opus-4-1-20250805"),
    Claude_Sonnet_4_6("claude-sonnet-4-6"),
    Claude_Sonnet_4_5("claude-sonnet-4-5-20250929"),
    Claude_Haiku_4_5("claude-haiku-4-5-20251001"),
}

/**
 * The C# ClaudeModelJsonConverter: a JSON string that .NET's `Enum.TryParse(name, ignoreCase: true)`
 * reads as a defined member is that member; anything else (null, a number, an unknown name) is None.
 */
object ClaudeModelSerializer : KSerializer<ClaudeModel> {
    override val descriptor = PrimitiveSerialDescriptor("ClaudeModel", PrimitiveKind.STRING)

    override fun serialize(encoder: Encoder, value: ClaudeModel) = encoder.encodeString(value.name)

    override fun deserialize(decoder: Decoder): ClaudeModel {
        val element = (decoder as JsonDecoder).decodeJsonElement()
        if (element !is JsonPrimitive || element is JsonNull || !element.isString) return ClaudeModel.None
        return tryParse(element.content)?.let { value -> ClaudeModel.entries.getOrNull(value) } ?: ClaudeModel.None
    }

    /**
     * .NET's `Enum.TryParse<ClaudeModel>(value, ignoreCase: true)`: the member's value, which may be
     * undefined, or null when it doesn't parse. Leading white space is skipped; then a value starting
     * with a digit or sign is an Int32 (trailing white space allowed), and anything else is member
     * names separated by commas, each trimmed, whose values are OR'd together.
     */
    internal fun tryParse(value: String): Int? {
        val text = DotNetString.trimStart(value)
        if (text.isEmpty()) return null

        val first = text[0]
        if (first in '0'..'9' || first == '-' || first == '+') return parseInt32(text)

        var result = 0
        for (part in text.split(',')) {
            val name = DotNetString.trim(part)
            val member = ClaudeModel.entries.firstOrNull { DotNetText.asciiFold(it.name) == DotNetText.asciiFold(name) }
                ?: return null
            result = result or member.ordinal
        }
        return result
    }

    /** Int32 with NumberStyles.AllowLeadingSign | AllowTrailingWhite, invariant culture. */
    private fun parseInt32(text: String): Int? {
        var end = text.length
        while (end > 0 && text[end - 1] == '\u0000') end--
        while (end > 0 && (text[end - 1] == ' ' || text[end - 1] in '\t'..'\r')) end--

        val number = text.substring(0, end)
        val digits = if (number.startsWith('-') || number.startsWith('+')) number.substring(1) else number
        if (digits.isEmpty() || !digits.all { it in '0'..'9' }) return null
        return number.toIntOrNull()
    }
}

@Serializable
data class AiPrompt(
    @SerialName("DisplayName") val displayName: String = "",
    @SerialName("Model") val model: ClaudeModel = ClaudeModel.Claude_Sonnet_4_5,
    /** {0} is replaced with the thread so far, {1} with the user's own instruction. */
    @SerialName("TextContent") val textContent: String = "",
) {
    override fun toString(): String = displayName
}

@Serializable
data class AIConfiguration(
    @SerialName("ApiKey") val apiKey: String? = null,
    @SerialName("Prompts") val prompts: List<AiPrompt> = emptyList(),
)

@Serializable
data class SavePathConfiguration(
    @SerialName("DefaultImageDirectory") val defaultImageDirectory: String? = null,
    @SerialName("DefaultVideoDirectory") val defaultVideoDirectory: String? = null,
)

@Serializable
data class SubRedditSubscription(
    @SerialName("DisplayString") val displayString: String? = null,
    @SerialName("Sort") val sort: PostSort = PostSort.Hot,
    @SerialName("SubReddit") val thingName: String? = null,
)

@Serializable
data class LandingPageConfiguration(@SerialName("Subscriptions") val subscriptions: List<SubRedditSubscription> = emptyList())

/** The C# app's session, as its export stores it. */
@Serializable
data class ExportedSession(
    @SerialName("AccessToken") val accessToken: String,
    @SerialName("AuthenticatedUser") val authenticatedUser: String,
    @SerialName("Expiration") val expiration: String? = null,
    @SerialName("SessionCookies") val sessionCookies: String? = null,
) {
    /**
     * As this app's session. C# wrote the expiration as a DateTime: with "Z" or an offset when it
     * knew the zone, without one otherwise, which its expiry check compared against UTC.
     */
    fun toSession(): Session = Session(accessToken, authenticatedUser, expiration?.let(::dotNetDateTimeToIso), sessionCookies)

    companion object {
        fun of(session: Session) = ExportedSession(session.accessToken, session.authenticatedUser, session.expiration, session.sessionCookies)

        private val zoned = Regex("(Z|[+-]\\d{2}:\\d{2})$")

        private fun dotNetDateTimeToIso(value: String): String = if (zoned.containsMatchIn(value)) {
            OffsetDateTime.parse(value).toInstant().toString()
        } else {
            LocalDateTime.parse(value).toInstant(ZoneOffset.UTC).toString()
        }
    }
}

/** The "Export Settings" file. */
@Serializable
data class SettingsExportData(
    @SerialName("ExportVersion") val exportVersion: String = "1.0",
    @SerialName("Styling") val styling: ApplicationStyling = ApplicationStyling(),
    @SerialName("AIConfiguration") val aiConfiguration: AIConfiguration = AIConfiguration(),
    @SerialName("BlockConfiguration") val blockConfiguration: BlockConfiguration = BlockConfiguration(),
    @SerialName("ApplicationHacks") val applicationHacks: ApplicationHacks = ApplicationHacks(),
    @SerialName("SavePaths") val savePaths: SavePathConfiguration = SavePathConfiguration(),
    @SerialName("LandingPageConfiguration") val landingPageConfiguration: LandingPageConfiguration = LandingPageConfiguration(),
    @SerialName("PostHistory") val postHistory: List<String> = emptyList(),
    @SerialName("BearerTokenJson") val bearerTokenJson: String? = null,
    @SerialName("UserTags") val userTags: Map<String, String> = emptyMap(),
)
