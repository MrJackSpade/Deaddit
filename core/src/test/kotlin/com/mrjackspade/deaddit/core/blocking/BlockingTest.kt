package com.mrjackspade.deaddit.core.blocking

import com.mrjackspade.deaddit.core.config.BlockConfiguration
import com.mrjackspade.deaddit.core.config.BlockList
import com.mrjackspade.deaddit.core.config.BlockRule
import com.mrjackspade.deaddit.core.config.BlockType
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.UserPartialData
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.time.Duration
import java.time.Instant

class BlockingTest {
    private val post = Post(
        id = "p", name = "t3_p", title = "A Meme about cats", author = "someone", authorFullname = "t2_a",
        subreddit = "pics", domain = "i.redd.it", linkFlairText = "Meme",
    )
    private val comment = Comment(id = "c", name = "t1_c", author = "someone", body = "I really like turtles", subreddit = "pics")

    @Test
    fun ruleMatchesWhenAllSetFieldsMatch() {
        assertTrue(Blocking.isMatch(BlockRule(flair = "meme", subReddit = "PICS"), post))
        assertFalse(Blocking.isMatch(BlockRule(flair = "meme", subReddit = "funny"), post))
    }

    @Test
    fun emptyRuleMatchesNothing() {
        assertFalse(Blocking.isMatch(BlockRule(), post))
        assertFalse(Blocking.isMatch(BlockRule(), comment))
    }

    @Test
    fun blockTypeLimitsWhatRulesApplyTo() {
        assertFalse(Blocking.isMatch(BlockRule(author = "someone", blockType = BlockType.Comment), post))
        assertFalse(Blocking.isMatch(BlockRule(author = "someone", blockType = BlockType.Post), comment))
        assertTrue(Blocking.isMatch(BlockRule(author = "someone", blockType = BlockType.Both), comment))
    }

    @Test
    fun titleAndBodyMatchPartiallyOrByRegex() {
        assertTrue(Blocking.isMatch(BlockRule(title = "CATS"), post))
        assertTrue(Blocking.isMatch(BlockRule(body = "turtles"), comment))
        assertTrue(Blocking.isMatch(BlockRule(title = "/M.me/"), post))
        // Regexes are case-sensitive, as in the C# app.
        assertFalse(Blocking.isMatch(BlockRule(title = "/meme/"), post))
    }

    @Test
    fun exactFieldsDontMatchPartially() {
        assertFalse(Blocking.isMatch(BlockRule(domain = "redd.it"), post))
        assertTrue(Blocking.isMatch(BlockRule(domain = "I.REDD.IT"), post))
    }

    @Test
    fun whitelistBeatsBlacklist() {
        val config = BlockConfiguration(
            blackList = BlockList(listOf(BlockRule(subReddit = "pics"))),
            whiteList = BlockList(listOf(BlockRule(author = "someone"))),
        )
        assertTrue(Blocking.isAllowed(config, post))
        assertFalse(Blocking.isAllowed(config.copy(whiteList = BlockList()), post))
    }

    @Test
    fun accountLimitsApplyOnlyWithUserData() {
        val now = Instant.parse("2026-11-01T00:00:00Z")
        val young = UserPartialData(name = "someone", linkKarma = 10, commentKarma = 5, createdUtc = now.minus(Duration.ofDays(3)))
        val data = mapOf("t2_a" to young)

        assertFalse(Blocking.isAllowed(BlockConfiguration(minAccountAgeDays = 7), post, data, now))
        assertTrue(Blocking.isAllowed(BlockConfiguration(minAccountAgeDays = 2), post, data, now))
        assertTrue(Blocking.isAllowed(BlockConfiguration(minAccountAgeDays = 7), post, null, now))
        assertFalse(Blocking.isAllowed(BlockConfiguration(minCommentKarma = 6), post, data, now))
        assertFalse(Blocking.isAllowed(BlockConfiguration(maxLinkKarma = 9), post, data, now))
        assertFalse(Blocking.isAllowed(BlockConfiguration(maxLinkKarmaRatio = 1.5), post, data, now))
        assertTrue(Blocking.isAllowed(BlockConfiguration(maxLinkKarmaRatio = 2.5), post, data, now))
    }
}
