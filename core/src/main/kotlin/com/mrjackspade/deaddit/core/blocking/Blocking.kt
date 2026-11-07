package com.mrjackspade.deaddit.core.blocking

import com.mrjackspade.deaddit.core.config.BlockConfiguration
import com.mrjackspade.deaddit.core.config.BlockRule
import com.mrjackspade.deaddit.core.config.BlockType
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.RedditThing
import com.mrjackspade.deaddit.core.model.UserPartialData
import java.time.Duration
import java.time.Instant

/**
 * Whether a field of a rule matched: [Skip] when the rule doesn't set it, so it takes no part.
 * A rule matches when at least one field matched and none failed (AND over the set fields).
 */
enum class TriggerState { Match, NoMatch, Skip }

private class MatchResult {
    var state = TriggerState.Skip
        private set

    val isMatch get() = state == TriggerState.Match

    fun apply(field: TriggerState) {
        when (field) {
            TriggerState.Match -> if (state == TriggerState.Skip) state = TriggerState.Match
            TriggerState.Skip -> Unit
            TriggerState.NoMatch -> state = TriggerState.NoMatch
        }
    }
}

object Blocking {
    /**
     * Whether to show [thing]. A whitelist match allows it outright; then a blacklist match
     * blocks it; then, when [userData] was fetched, the account limits apply.
     */
    fun isAllowed(config: BlockConfiguration, thing: RedditThing, userData: Map<String, UserPartialData>? = null, now: Instant = Instant.now()): Boolean {
        if (config.whiteList.rules.any { isMatch(it, thing) }) return true
        if (config.blackList.rules.any { isMatch(it, thing) }) return false

        val user = thing.authorFullname?.let { userData?.get(it) }

        if (user != null) {
            if (config.minAccountAgeDays > 0 && user.createdUtc != null &&
                Duration.between(user.createdUtc, now).toMillis() / 86_400_000.0 < config.minAccountAgeDays
            ) return false

            if (config.minCommentKarma > 0 && config.minCommentKarma > user.commentKarma) return false

            if (config.maxLinkKarma > 0 && config.maxLinkKarma < user.linkKarma) return false

            if (config.maxLinkKarmaRatio > 0 && user.linkKarma > 0 &&
                user.linkKarma.toDouble() / user.commentKarma > config.maxLinkKarmaRatio
            ) return false
        }

        return true
    }

    /** Whether fetching authors' account data is needed to apply [config]. */
    fun requiresUserData(config: BlockConfiguration): Boolean =
        config.minAccountAgeDays > 0 || config.minCommentKarma > 0 || config.maxLinkKarma > 0

    fun isMatch(rule: BlockRule, thing: RedditThing): Boolean {
        val result = MatchResult()

        if (thing is Post) {
            if (rule.blockType == BlockType.Comment) return false
            result.apply(postIsMatch(rule, thing))
        }

        if (thing is Comment && rule.blockType == BlockType.Post) return false

        result.apply(triggersOrSkip(rule.author, thing.author, matchType(rule.author)))
        result.apply(triggersOrSkip(rule.body, thing.body, matchType(rule.body), partial = true))

        return result.isMatch
    }

    private fun postIsMatch(rule: BlockRule, post: Post): TriggerState {
        val result = MatchResult()
        result.apply(triggersOrSkip(rule.domain, post.domain, StringMatchType.String))
        result.apply(triggersOrSkip(rule.isLocked, post.locked))
        result.apply(triggersOrSkip(rule.isNsfw, post.isNsfw))
        result.apply(triggersOrSkip(rule.isArchived, post.archived))
        result.apply(triggersOrSkip(rule.flair, post.linkFlairText, StringMatchType.String))
        result.apply(triggersOrSkip(rule.subReddit, post.subreddit, StringMatchType.String))
        result.apply(triggersOrSkip(rule.title, post.title, matchType(rule.title), partial = true))
        return result.state
    }

    enum class StringMatchType { String, Regex }

    /** A value wrapped in /slashes/ is a regular expression. */
    private fun matchType(value: String?): StringMatchType =
        if (!value.isNullOrBlank() && value.startsWith('/') && value.endsWith('/')) StringMatchType.Regex else StringMatchType.String

    fun triggersOrSkip(ruleValue: Boolean, checkValue: Boolean?): TriggerState = when {
        !ruleValue -> TriggerState.Skip
        checkValue == true -> TriggerState.Match
        else -> TriggerState.NoMatch
    }

    /**
     * An unset [ruleValue] skips; an empty [checkValue] fails. Strings compare case-insensitively,
     * exactly or (with [partial]) as a substring. Regexes are case-sensitive, as in the C# app.
     */
    fun triggersOrSkip(ruleValue: String?, checkValue: String?, matchType: StringMatchType, partial: Boolean = false): TriggerState {
        if (ruleValue.isNullOrBlank()) return TriggerState.Skip
        if (checkValue.isNullOrBlank()) return TriggerState.NoMatch

        val match = when (matchType) {
            StringMatchType.Regex -> Regex(ruleValue.trim('/')).containsMatchIn(checkValue)
            StringMatchType.String -> if (partial) checkValue.contains(ruleValue, ignoreCase = true) else ruleValue.equals(checkValue, ignoreCase = true)
        }

        return if (match) TriggerState.Match else TriggerState.NoMatch
    }

    // ----- Rules made from a post or comment ("Block..." / "Whitelist...") -----

    fun fromAuthor(thing: RedditThing) = BlockRule(author = thing.author, blockType = BlockType.Both, ruleName = "/u/${thing.author}")

    fun fromDomain(post: Post) = BlockRule(domain = post.domain, blockType = BlockType.Post, ruleName = "(${post.domain})")

    fun fromFlair(post: Post) = BlockRule(
        flair = post.linkFlairText,
        subReddit = post.subreddit,
        blockType = BlockType.Post,
        ruleName = "${post.subreddit} [${post.linkFlairText}]",
    )

    fun fromFlair(comment: Comment) = BlockRule(
        flair = comment.authorFlairText,
        subReddit = comment.subreddit,
        blockType = BlockType.Post,
        ruleName = "${comment.subreddit} [${comment.authorFlairText}]",
    )

    fun fromSubReddit(thing: RedditThing) = BlockRule(subReddit = thing.subreddit, blockType = BlockType.Post, ruleName = "/r/${thing.subreddit}")
}
