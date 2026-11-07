package com.mrjackspade.deaddit.core.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import java.time.Instant

@Serializable
data class Subreddit(
    val id: String = "",
    val name: String = "",
    @SerialName("display_name") val displayName: String,
    @SerialName("display_name_prefixed") val displayNamePrefixed: String = "",
    val title: String? = null,
    @SerialName("public_description") val publicDescription: String? = null,
    val description: String? = null,
    @SerialName("description_html") val descriptionHtml: String? = null,
    val subscribers: Int = 0,
    @SerialName("active_user_count") val activeUserCount: Int? = null,
    @SerialName("created_utc") @Serializable(RedditInstantSerializer::class) val createdUtc: Instant? = null,
    val over18: Boolean = false,
    @SerialName("icon_img") @Serializable(BlankAsNullSerializer::class) val iconImg: String? = null,
    @SerialName("community_icon") @Serializable(BlankAsNullSerializer::class) val communityIcon: String? = null,
    @SerialName("banner_img") @Serializable(BlankAsNullSerializer::class) val bannerImg: String? = null,
    @SerialName("user_is_subscriber") val userIsSubscriber: Boolean? = null,
    @SerialName("user_is_moderator") val userIsModerator: Boolean? = null,
    @SerialName("submit_text") val submitText: String? = null,
)

@Serializable
data class User(
    val id: String = "",
    val name: String,
    @SerialName("link_karma") val linkKarma: Int = 0,
    @SerialName("comment_karma") val commentKarma: Int = 0,
    @SerialName("total_karma") val totalKarma: Int? = null,
    @SerialName("created_utc") @Serializable(RedditInstantSerializer::class) val createdUtc: Instant? = null,
    @SerialName("icon_img") @Serializable(BlankAsNullSerializer::class) val iconImg: String? = null,
    @SerialName("snoovatar_img") @Serializable(BlankAsNullSerializer::class) val snoovatarImg: String? = null,
    @SerialName("is_friend") val isFriend: Boolean? = null,
    @SerialName("is_mod") val isMod: Boolean = false,
    @SerialName("is_gold") val isGold: Boolean = false,
    /** The user's profile, which Reddit models as a subreddit. */
    val subreddit: UserProfile? = null,
)

@Serializable
data class UserProfile(@SerialName("public_description") val publicDescription: String? = null)

/** What /api/user_data_by_account_ids gives for each account. */
@Serializable
data class UserPartialData(
    val name: String,
    @SerialName("link_karma") val linkKarma: Int = 0,
    @SerialName("comment_karma") val commentKarma: Int = 0,
    @SerialName("created_utc") @Serializable(RedditInstantSerializer::class) val createdUtc: Instant? = null,
    @SerialName("profile_img") @Serializable(BlankAsNullSerializer::class) val profileImg: String? = null,
)

@Serializable
data class MultiSubreddit(val name: String)

@Serializable
data class Multi(
    val name: String,
    @SerialName("display_name") val displayName: String,
    val path: String,
    val owner: String = "",
    val visibility: String = "private",
    val subreddits: List<MultiSubreddit> = emptyList(),
)

@Serializable
data class SubredditRule(
    @SerialName("short_name") val shortName: String,
    val description: String? = null,
    @SerialName("violation_reason") val violationReason: String? = null,
    val kind: String = "",
    val priority: Int = 0,
)

@Serializable
data class SubredditRules(
    val rules: List<SubredditRule> = emptyList(),
    @SerialName("site_rules") val siteRules: List<String> = emptyList(),
)
