using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    [DebuggerDisplay("{Title}")]
    public class ApiPost : ApiThing
    {
        [JsonPropertyName("allow_live_comments")]
        public bool AllowLiveComments { get; init; }

        [JsonPropertyName("category")]
        public object? Category { get; init; }

        [JsonPropertyName("clicked")]
        public bool Clicked { get; init; }

        [JsonPropertyName("content_categories")]
        public List<string> ContentCategories { get; init; } = [];

        [JsonPropertyName("contest_mode")]
        public bool ContestMode { get; init; }

        [JsonPropertyName("discussion_type")]
        public object? DiscussionType { get; init; }

        [JsonPropertyName("domain")]
        public string? Domain { get; init; }

        [JsonPropertyName("gallery_data")]
        public GalleryData? GalleryData { get; init; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; init; }

        [JsonPropertyName("hide_score")]
        public bool HideScore { get; init; }

        [JsonPropertyName("is_created_from_ads_ui")]
        public bool IsCreatedFromAdsUi { get; init; }

        [JsonPropertyName("is_crosspostable")]
        public bool IsCrossPostable { get; init; }

        [JsonPropertyName("is_gallery")]
        public bool? IsGallery { get; init; }

        [JsonPropertyName("is_meta")]
        public bool IsMeta { get; init; }

        [JsonPropertyName("over_18")]
        public bool IsNsfw { get; init; }

        [JsonPropertyName("is_original_content")]
        public bool IsOriginalContent { get; init; }

        [JsonPropertyName("is_reddit_media_domain")]
        public bool IsRedditMediaDomain { get; init; }

        [JsonPropertyName("is_robot_indexable")]
        public bool IsRobotIndexable { get; init; }

        [JsonPropertyName("is_self")]
        public bool IsSelf { get; init; }

        [JsonPropertyName("is_video")]
        public bool IsVideo { get; init; }

        [JsonPropertyName("link_flair_background_color")]
        public Color? LinkFlairBackgroundColor { get; init; }

        [JsonPropertyName("link_flair_css_class")]
        public string? LinkFlairCssClass { get; init; }

        [JsonPropertyName("link_flair_richtext")]
        public List<LinkFlair> LinkFlairRichText { get; init; } = [];

        [JsonPropertyName("link_flair_template_id")]
        public string? LinkFlairTemplateId { get; init; }

        [JsonPropertyName("link_flair_text")]
        public string? LinkFlairText { get; init; }

        [JsonPropertyName("link_flair_text_color")]
        public Color? LinkFlairTextColor { get; init; }

        [JsonPropertyName("link_flair_type")]
        public string? LinkFlairType { get; init; }

        [JsonPropertyName("media")]
        public Media? Media { get; init; }

        [JsonPropertyName("media_embed")]
        public MediaEmbed? MediaEmbed { get; init; }

        [JsonPropertyName("media_only")]
        public bool MediaOnly { get; init; }

        [JsonPropertyName("num_comments")]
        public long NumComments { get; init; }

        [JsonPropertyName("num_crossposts")]
        public long NumCrossPosts { get; init; }

        [JsonPropertyName("parent_whitelist_status")]
        public string? ParentWhitelistStatus { get; init; }

        [JsonPropertyName("pinned")]
        public bool Pinned { get; init; }

        [JsonPropertyName("post_hint")]
        public string? PostHint { get; init; }

        [JsonPropertyName("preview")]
        public Preview? Preview { get; init; }

        [JsonPropertyName("pwls")]
        public long? Pwls { get; init; }

        [JsonPropertyName("quarantine")]
        public bool Quarantine { get; init; }

        [JsonPropertyName("removed_by")]
        public object? RemovedBy { get; init; }

        [JsonPropertyName("removed_by_category")]
        public object? RemovedByCategory { get; init; }

        [JsonPropertyName("secure_media")]
        public SecureMedia? SecureMedia { get; init; }

        [JsonPropertyName("secure_media_embed")]
        public SecureMediaEmbed? SecureMediaEmbed { get; init; }

        [JsonPropertyName("spoiler")]
        public bool Spoiler { get; init; }

        [JsonPropertyName("subreddit_subscribers")]
        public long SubredditSubscribers { get; init; }

        [JsonPropertyName("suggested_sort")]
        public ApiPostSort? SuggestedSort { get; init; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; init; }

        [JsonPropertyName("thumbnail_height")]
        public int? ThumbnailHeight { get; init; }

        [JsonPropertyName("thumbnail_width")]
        public int? ThumbnailWidth { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("upvote_ratio")]
        public double UpvoteRatio { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("url_overridden_by_dest")]
        public string? UrlOverriddenByDest { get; init; }

        [JsonPropertyName("view_count")]
        public object? ViewCount { get; init; }

        [JsonPropertyName("visited")]
        public bool Visited { get; init; }

        [JsonPropertyName("whitelist_status")]
        public string? WhitelistStatus { get; init; }

        [JsonPropertyName("wls")]
        public long? Wls { get; init; }
    }
}