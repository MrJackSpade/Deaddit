using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    [DebuggerDisplay("{Title}")]
    public class RedditPost : RedditThing
    {
        [JsonPropertyName("allow_live_comments")]
        public bool AllowLiveComments { get; set; }

        [JsonPropertyName("category")]
        public object? Category { get; set; }

        [JsonPropertyName("clicked")]
        public bool Clicked { get; set; }

        [JsonPropertyName("content_categories")]
        public List<string> ContentCategories { get; set; } = [];

        [JsonPropertyName("contest_mode")]
        public bool ContestMode { get; set; }

        [JsonPropertyName("discussion_type")]
        public object? DiscussionType { get; set; }

        [JsonPropertyName("domain")]
        public string? Domain { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

        [JsonPropertyName("hide_score")]
        public bool HideScore { get; set; }

        [JsonPropertyName("is_created_from_ads_ui")]
        public bool IsCreatedFromAdsUi { get; set; }

        [JsonPropertyName("is_crosspostable")]
        public bool IsCrossPostable { get; set; }

        [JsonPropertyName("is_meta")]
        public bool IsMeta { get; set; }

        [JsonPropertyName("is_original_content")]
        public bool IsOriginalContent { get; set; }

        [JsonPropertyName("is_reddit_media_domain")]
        public bool IsRedditMediaDomain { get; set; }

        [JsonPropertyName("is_robot_indexable")]
        public bool IsRobotIndexable { get; set; }

        [JsonPropertyName("is_self")]
        public bool IsSelf { get; set; }

        [JsonPropertyName("is_video")]
        public bool IsVideo { get; set; }

        [JsonPropertyName("link_flair_background_color")]
        public Color? LinkFlairBackgroundColor { get; set; }

        [JsonPropertyName("link_flair_css_class")]
        public string? LinkFlairCssClass { get; set; }

        [JsonPropertyName("link_flair_richtext")]
        public List<LinkFlair> LinkFlairRichText { get; set; } = [];

        [JsonPropertyName("link_flair_template_id")]
        public string? LinkFlairTemplateId { get; set; }

        [JsonPropertyName("link_flair_text")]
        public string? LinkFlairText { get; set; }

        [JsonPropertyName("link_flair_text_color")]
        public Color? LinkFlairTextColor { get; set; }

        [JsonPropertyName("link_flair_type")]
        public string? LinkFlairType { get; set; }

        [JsonPropertyName("media")]
        public Media? Media { get; set; }

        [JsonPropertyName("media_embed")]
        public MediaEmbed? MediaEmbed { get; set; }

        [JsonPropertyName("media_only")]
        public bool MediaOnly { get; set; }

        [JsonPropertyName("num_comments")]
        public long NumComments { get; set; }

        [JsonPropertyName("num_crossposts")]
        public long NumCrossPosts { get; set; }

        [JsonPropertyName("over_18")]
        public bool Over18 { get; set; }

        [JsonPropertyName("parent_whitelist_status")]
        public string? ParentWhitelistStatus { get; set; }

        [JsonPropertyName("pinned")]
        public bool Pinned { get; set; }

        [JsonPropertyName("post_hint")]
        public string? PostHint { get; set; }

        [JsonPropertyName("preview")]
        public Preview? Preview { get; set; }

        [JsonPropertyName("pwls")]
        public long? Pwls { get; set; }

        [JsonPropertyName("quarantine")]
        public bool Quarantine { get; set; }

        [JsonPropertyName("removed_by")]
        public object? RemovedBy { get; set; }

        [JsonPropertyName("removed_by_category")]
        public object? RemovedByCategory { get; set; }

        [JsonPropertyName("secure_media")]
        public SecureMedia? SecureMedia { get; set; }

        [JsonPropertyName("secure_media_embed")]
        public SecureMediaEmbed? SecureMediaEmbed { get; set; }

        [JsonPropertyName("spoiler")]
        public bool Spoiler { get; set; }

        [JsonPropertyName("subreddit_subscribers")]
        public long SubredditSubscribers { get; set; }

        [JsonPropertyName("suggested_sort")]
        public string? SuggestedSort { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("thumbnail_height")]
        public int? ThumbnailHeight { get; set; }

        [JsonPropertyName("thumbnail_width")]
        public int? ThumbnailWidth { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("upvote_ratio")]
        public double UpvoteRatio { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("url_overridden_by_dest")]
        public string? UrlOverriddenByDest { get; set; }

        [JsonPropertyName("view_count")]
        public object? ViewCount { get; set; }

        [JsonPropertyName("visited")]
        public bool Visited { get; set; }

        [JsonPropertyName("whitelist_status")]
        public string? WhitelistStatus { get; set; }

        [JsonPropertyName("wls")]
        public long? Wls { get; set; }
    }
}