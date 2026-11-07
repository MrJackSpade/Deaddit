package com.mrjackspade.deaddit.ui.editor

import com.mrjackspade.deaddit.core.config.AIConfiguration
import com.mrjackspade.deaddit.core.config.AiPrompt
import com.mrjackspade.deaddit.core.config.ApplicationHacks
import com.mrjackspade.deaddit.core.config.ApplicationStyling
import com.mrjackspade.deaddit.core.config.BlockConfiguration
import com.mrjackspade.deaddit.core.config.BlockList
import com.mrjackspade.deaddit.core.config.BlockRule
import com.mrjackspade.deaddit.core.config.BlockType
import com.mrjackspade.deaddit.core.config.ClaudeModel
import com.mrjackspade.deaddit.core.config.OptionalStrip
import com.mrjackspade.deaddit.core.config.PostState
import com.mrjackspade.deaddit.core.config.Region

/** What "Edit Configuration" edits (the C# EditorConfiguration): the four settings saved together. */
data class EditorConfiguration(
    val ai: AIConfiguration,
    val hacks: ApplicationHacks,
    val blocking: BlockConfiguration,
    val styling: ApplicationStyling,
)

/**
 * The C# models' [EditorDisplay] attributes, field for field: same labels (typos included), orders
 * and descriptions, listed in the C# declaration order.
 */
object Schemas {
    val blockRule: Schema<BlockRule> = schema {
        text("Author", 3, get = { it.author }, set = { o, v -> o.copy(author = v) })
        enum("Block Type", BlockType.entries, 1, get = { it.blockType }, set = { o, v -> o.copy(blockType = v) })
        text("Body", 7, get = { it.body }, set = { o, v -> o.copy(body = v) })
        text("Domain", 7, get = { it.domain }, set = { o, v -> o.copy(domain = v) })
        text("Flair", 8, get = { it.flair }, set = { o, v -> o.copy(flair = v) })
        bool("Archived", 4, get = { it.isArchived }, set = { o, v -> o.copy(isArchived = v) })
        bool("Locked", 5, get = { it.isLocked }, set = { o, v -> o.copy(isLocked = v) })
        bool("NSFW", 4, get = { it.isNsfw }, set = { o, v -> o.copy(isNsfw = v) })
        text("Rule Name", 0, get = { it.ruleName }, set = { o, v -> o.copy(ruleName = v) })
        text("SubReddit", 2, get = { it.subReddit }, set = { o, v -> o.copy(subReddit = v) })
        text("Title", 6, get = { it.title }, set = { o, v -> o.copy(title = v) })
    }

    private val blockList: Schema<BlockList> = schema {
        list("Rules", blockRule, ::BlockRule, itemLabel = { it.ruleName }, get = { it.rules }, set = { o, v -> o.copy(rules = v) })
    }

    val blocking: Schema<BlockConfiguration> = schema {
        obj("Blacklist", blockList, 1, get = { it.blackList }, set = { o, v -> o.copy(blackList = v) })
        enum("Duplicate Link Handling", PostState.entries, get = { it.duplicateLinkHandling }, set = { o, v -> o.copy(duplicateLinkHandling = v) })
        enum(
            "Duplicate Thumb Handling", PostState.entries,
            description = "Uses the thumbnail ETAG to determine if posts link to the same content, even when the url differs",
            get = { it.duplicateThumbHandling }, set = { o, v -> o.copy(duplicateThumbHandling = v) },
        )
        enum("Duplicate Title Handling", PostState.entries, get = { it.duplicateTitleHandling }, set = { o, v -> o.copy(duplicateTitleHandling = v) })
        int("Maximum Link Karma", get = { it.maxLinkKarma }, set = { o, v -> o.copy(maxLinkKarma = v) })
        double("Maximum Link Karma Ratio", get = { it.maxLinkKarmaRatio }, set = { o, v -> o.copy(maxLinkKarmaRatio = v) })
        int("Mininum Account Age (Days)", get = { it.minAccountAgeDays }, set = { o, v -> o.copy(minAccountAgeDays = v) })
        int("Minimum Comment Karma", get = { it.minCommentKarma }, set = { o, v -> o.copy(minCommentKarma = v) })
        obj("Whitelist", blockList, 1, get = { it.whiteList }, set = { o, v -> o.copy(whiteList = v) })
    }

    val hacks: Schema<ApplicationHacks> = schema {
        bool("Auto Load Comment Images", 0, get = { it.autoLoadCommentImages }, set = { o, v -> o.copy(autoLoadCommentImages = v) })
        bool("Convert GIFs to MP4 on Save/Share", 1, get = { it.convertGifsToMp4 }, set = { o, v -> o.copy(convertGifsToMp4 = v) })
        enum("Comment Emoji Handling", OptionalStrip.entries, 3, get = { it.commentEmojiHandling }, set = { o, v -> o.copy(commentEmojiHandling = v) })
        enum("Default Region", Region.entries, 4, get = { it.defaultRegion }, set = { o, v -> o.copy(defaultRegion = v) })
        bool("Skip Resolving Flair Images", 1, get = { it.skipResolvingFlairImages }, set = { o, v -> o.copy(skipResolvingFlairImages = v) })
        int("Minimum Comment Body Length", 4, get = { it.minimumCommentBodyLength }, set = { o, v -> o.copy(minimumCommentBodyLength = v) })
        int("Minimum Post Body Length", 4, get = { it.minimumPostBodyLength }, set = { o, v -> o.copy(minimumPostBodyLength = v) })
        int("Page Buffer", 6, get = { it.pageBuffer }, set = { o, v -> o.copy(pageBuffer = v) })
        int("Page Size", 5, get = { it.pageSize }, set = { o, v -> o.copy(pageSize = v) })
        enum("Title Emoji Handling", OptionalStrip.entries, 2, get = { it.titleEmojiHandling }, set = { o, v -> o.copy(titleEmojiHandling = v) })
        bool("Hide Self Karma", 7, get = { it.hideSelfKarma }, set = { o, v -> o.copy(hideSelfKarma = v) })
        enum("Title Newlines", OptionalStrip.entries, 4, get = { it.titleNewlines }, set = { o, v -> o.copy(titleNewlines = v) })
        bool("Keep Alive (Background Service)", 8, get = { it.keepAlive }, set = { o, v -> o.copy(keepAlive = v) })
        bool("Swear Obfuscation", 9, get = { it.swearObfuscation }, set = { o, v -> o.copy(swearObfuscation = v) })
    }

    val styling: Schema<ApplicationStyling> = schema {
        color("Admin Author Background Color", 4, get = { it.adminAuthorBackgroundColor }, set = { o, v -> o.copy(adminAuthorBackgroundColor = v) })
        color("Admin Author Text Color", 4, get = { it.adminAuthorTextColor }, set = { o, v -> o.copy(adminAuthorTextColor = v) })
        color("Admin Title Text Color", 4, get = { it.adminTitleTextColor }, set = { o, v -> o.copy(adminTitleTextColor = v) })
        color("Blocked Background Color", 13, get = { it.blockedBackgroundColor }, set = { o, v -> o.copy(blockedBackgroundColor = v) })
        double("Comment Font Size", 11, get = { it.commentFontSize }, set = { o, v -> o.copy(commentFontSize = v) })
        color("Distinguished Title Color", 4, get = { it.distinguishedTitleColor }, set = { o, v -> o.copy(distinguishedTitleColor = v) })
        color("Downvote Color", 10, get = { it.downvoteColor }, set = { o, v -> o.copy(downvoteColor = v) })
        color("Flair Dark Text Color", 8, get = { it.flairDarkTextColor }, set = { o, v -> o.copy(flairDarkTextColor = v) })
        color("Flair Light Text Color", 8, get = { it.flairLightTextColor }, set = { o, v -> o.copy(flairLightTextColor = v) })
        bool("Swap Flair Text and Background Colors", 8, get = { it.swapFlairColors }, set = { o, v -> o.copy(swapFlairColors = v) })
        color("Highlight Color", 5, get = { it.highlightColor }, set = { o, v -> o.copy(highlightColor = v) })
        color("Hyperlink Color", 8, get = { it.hyperlinkColor }, set = { o, v -> o.copy(hyperlinkColor = v) })
        color("Moderator Author Background Color", 4, get = { it.moderatorAuthorBackgroundColor }, set = { o, v -> o.copy(moderatorAuthorBackgroundColor = v) })
        color("Moderator Author Text Color", 4, get = { it.moderatorAuthorTextColor }, set = { o, v -> o.copy(moderatorAuthorTextColor = v) })
        color("Moderator Title Text Color", 4, get = { it.moderatorTitleTextColor }, set = { o, v -> o.copy(moderatorTitleTextColor = v) })
        color("Op Background Color", get = { it.opBackgroundColor }, set = { o, v -> o.copy(opBackgroundColor = v) })
        color("Op Text Color", get = { it.opTextColor }, set = { o, v -> o.copy(opTextColor = v) })
        color("Primary Color", 1, get = { it.primaryColor }, set = { o, v -> o.copy(primaryColor = v) })
        color("Secondary Color", 2, get = { it.secondaryColor }, set = { o, v -> o.copy(secondaryColor = v) })
        color("Subtext Color", 7, get = { it.subTextColor }, set = { o, v -> o.copy(subTextColor = v) })
        double("SubText Font Size", 11, get = { it.subTextFontSize }, set = { o, v -> o.copy(subTextFontSize = v) })
        color("Tertiary Color", 3, get = { it.tertiaryColor }, set = { o, v -> o.copy(tertiaryColor = v) })
        color("Text Color", 6, get = { it.textColor }, set = { o, v -> o.copy(textColor = v) })
        int("Thumbnail Size", 12, get = { it.thumbnailSize }, set = { o, v -> o.copy(thumbnailSize = v) })
        double("Title Font Size", 11, get = { it.titleFontSize }, set = { o, v -> o.copy(titleFontSize = v) })
        color("Upvote Color", 9, get = { it.upvoteColor }, set = { o, v -> o.copy(upvoteColor = v) })
        double("Visited Opacity", 13, get = { it.visitedOpacity }, set = { o, v -> o.copy(visitedOpacity = v) })
    }

    private val aiPrompt: Schema<AiPrompt> = schema {
        text("Display Name", description = "The name of the prompt as shown in the UI", get = { it.displayName }, set = { o, v -> o.copy(displayName = v) })
        enum("Model", ClaudeModel.entries, description = "The Claude model to use for this prompt", get = { it.model }, set = { o, v -> o.copy(model = v) })
        text(
            "Text Content",
            description = "The prompt text to be sent to the AI model. Use {0} to inject history and {1} to inject a custom user command",
            multiline = true,
            get = { it.textContent }, set = { o, v -> o.copy(textContent = v) },
        )
    }

    val ai: Schema<AIConfiguration> = schema {
        text("Api Key", get = { it.apiKey }, set = { o, v -> o.copy(apiKey = v) })
        // AiPrompt.ToString() is its DisplayName, never null.
        list("Prompts", aiPrompt, ::AiPrompt, itemLabel = { it.displayName }, get = { it.prompts }, set = { o, v -> o.copy(prompts = v) })
    }

    val configuration: Schema<EditorConfiguration> = schema {
        obj("AI Configuration", ai, get = { it.ai }, set = { o, v -> o.copy(ai = v) })
        obj("Hacks", hacks, get = { it.hacks }, set = { o, v -> o.copy(hacks = v) })
        obj("Block Configuration", blocking, get = { it.blocking }, set = { o, v -> o.copy(blocking = v) })
        obj("Styling", styling, get = { it.styling }, set = { o, v -> o.copy(styling = v) })
    }
}
