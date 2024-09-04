using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class ApiSubReddit : ApiThing
    {
        [JsonPropertyName("accept_followers")]
        public bool AcceptFollowers { get; init; }

        [JsonPropertyName("accounts_active")]
        public int AccountsActive { get; init; }

        [JsonPropertyName("accounts_active_is_fuzzed")]
        public bool AccountsActiveIsFuzzed { get; init; }

        [JsonPropertyName("active_user_count")]
        public int ActiveUserCount { get; init; }

        [JsonPropertyName("advertiser_category")]
        public string? AdvertiserCategory { get; init; }

        [JsonPropertyName("all_original_content")]
        public bool AllOriginalContent { get; init; }

        [JsonPropertyName("allow_discovery")]
        public bool AllowDiscovery { get; init; }

        [JsonPropertyName("allowed_media_in_comments")]
        public List<string> AllowedMediaInComments { get; init; } = [];

        [JsonPropertyName("allow_galleries")]
        public bool AllowGalleries { get; init; }

        [JsonPropertyName("allow_images")]
        public bool AllowImages { get; init; }

        [JsonPropertyName("allow_polls")]
        public bool AllowPolls { get; init; }

        [JsonPropertyName("allow_prediction_contributors")]
        public bool AllowPredictionContributors { get; init; }

        [JsonPropertyName("allow_predictions")]
        public bool AllowPredictions { get; init; }

        [JsonPropertyName("allow_predictions_tournament")]
        public bool AllowPredictionsTournament { get; init; }

        [JsonPropertyName("allow_talks")]
        public bool AllowTalks { get; init; }

        [JsonPropertyName("allow_videogifs")]
        public bool AllowVideoGifs { get; init; }

        [JsonPropertyName("allow_videos")]
        public bool AllowVideos { get; init; }

        [JsonPropertyName("banner_background_color")]
        public string? BannerBackgroundColor { get; init; }

        [JsonPropertyName("banner_background_image")]
        public string? BannerBackgroundImage { get; init; }

        [JsonPropertyName("banner_img")]
        public string? BannerImg { get; init; }

        [JsonPropertyName("banner_size")]
        public object? BannerSize { get; init; }

        [JsonPropertyName("can_assign_link_flair")]
        public bool CanAssignLinkFlair { get; init; }

        [JsonPropertyName("can_assign_user_flair")]
        public bool CanAssignUserFlair { get; init; }

        [JsonPropertyName("collapse_deleted_comments")]
        public bool CollapseDeletedComments { get; init; }

        [JsonPropertyName("comment_contribution_settings")]
        public CommentContributionSettings? CommentContributionSettings { get; init; }

        [JsonPropertyName("comment_score_hide_mins")]
        public int CommentScoreHideMins { get; init; }

        [JsonPropertyName("community_icon")]
        public string? CommunityIcon { get; init; }

        [JsonPropertyName("community_reviewed")]
        public bool CommunityReviewed { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("description_html")]
        public string? DescriptionHtml { get; init; }

        [JsonPropertyName("disable_contributor_requests")]
        public bool DisableContributorRequests { get; init; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("display_name_prefixed")]
        public string? DisplayNamePrefixed { get; init; }

        [JsonPropertyName("emojis_custom_size")]
        public object? EmojisCustomSize { get; init; }

        [JsonPropertyName("emojis_enabled")]
        public bool EmojisEnabled { get; init; }

        [JsonPropertyName("free_form_reports")]
        public bool FreeFormReports { get; init; }

        [JsonPropertyName("has_menu_widget")]
        public bool HasMenuWidget { get; init; }

        [JsonPropertyName("header_img")]
        public object? HeaderImg { get; init; }

        [JsonPropertyName("header_size")]
        public object? HeaderSize { get; init; }

        [JsonPropertyName("header_title")]
        public string? HeaderTitle { get; init; }

        [JsonPropertyName("hide_ads")]
        public bool HideAds { get; init; }

        [JsonPropertyName("icon_img")]
        public string? IconImg { get; init; }

        [JsonPropertyName("icon_size")]
        public object? IconSize { get; init; }

        [JsonPropertyName("is_crosspostable_subreddit")]
        public object? IsCrosspostableSubreddit { get; init; }

        [JsonPropertyName("is_enrolled_in_new_modmail")]
        public bool? IsEnrolledInNewModMail { get; init; }

        [JsonPropertyName("over18")]
        public bool IsNSFW { get; init; }

        [JsonPropertyName("key_color")]
        public Color? KeyColor { get; init; }

        [JsonPropertyName("lang")]
        public string? Lang { get; init; }

        [JsonPropertyName("link_flair_enabled")]
        public bool LinkFlairEnabled { get; init; }

        [JsonPropertyName("link_flair_position")]
        public string? LinkFlairPosition { get; init; }

        [JsonPropertyName("mobile_banner_image")]
        public string? MobileBannerImage { get; init; }

        [JsonPropertyName("notification_level")]
        public string? NotificationLevel { get; init; }

        [JsonPropertyName("original_content_tag_enabled")]
        public bool OriginalContentTagEnabled { get; init; }

        [JsonPropertyName("prediction_leaderboard_entry_type")]
        public int PredictionLeaderboardEntryType { get; init; }

        [JsonPropertyName("primary_color")]
        public string? PrimaryColor { get; init; }

        [JsonPropertyName("public_description")]
        public string? PublicDescription { get; init; }

        [JsonPropertyName("public_description_html")]
        public string? PublicDescriptionHtml { get; init; }

        [JsonPropertyName("public_traffic")]
        public bool PublicTraffic { get; init; }

        [JsonPropertyName("quarantine")]
        public bool Quarantine { get; init; }

        [JsonPropertyName("restrict_commenting")]
        public bool RestrictCommenting { get; init; }

        [JsonPropertyName("restrict_posting")]
        public bool RestrictPosting { get; init; }

        [JsonPropertyName("should_archive_posts")]
        public bool ShouldArchivePosts { get; init; }

        [JsonPropertyName("should_show_media_in_comments_setting")]
        public bool ShouldShowMediaInCommentsSetting { get; init; }

        [JsonPropertyName("show_media")]
        public bool ShowMedia { get; init; }

        [JsonPropertyName("show_media_preview")]
        public bool ShowMediaPreview { get; init; }

        [JsonPropertyName("spoilers_enabled")]
        public bool SpoilersEnabled { get; init; }

        [JsonPropertyName("submission_type")]
        public string? SubmissionType { get; init; }

        [JsonPropertyName("submit_link_label")]
        public string? SubmitLinkLabel { get; init; }

        [JsonPropertyName("submit_text")]
        public string? SubmitText { get; init; }

        [JsonPropertyName("submit_text_html")]
        public object? SubmitTextHtml { get; init; }

        [JsonPropertyName("submit_text_label")]
        public string? SubmitTextLabel { get; init; }

        [JsonPropertyName("subscribers")]
        public int Subscribers { get; init; }

        [JsonPropertyName("suggested_comment_sort")]
        public string? SuggestedCommentSort { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("user_can_flair_in_sr")]
        public bool UserCanFlairInSr { get; init; }

        [JsonPropertyName("user_flair_background_color")]
        public object? UserFlairBackgroundColor { get; init; }

        [JsonPropertyName("user_flair_css_class")]
        public string? UserFlairCssClass { get; init; }

        [JsonPropertyName("user_flair_enabled_in_sr")]
        public bool UserFlairEnabledInSr { get; init; }

        [JsonPropertyName("user_flair_position")]
        public string? UserFlairPosition { get; init; }

        [JsonPropertyName("user_flair_richtext")]
        public List<object> UserFlairRichtext { get; init; } = [];

        [JsonPropertyName("user_flair_template_id")]
        public object? UserFlairTemplateId { get; init; }

        [JsonPropertyName("user_flair_text")]
        public object? UserFlairText { get; init; }

        [JsonPropertyName("user_flair_text_color")]
        public Color? UserFlairTextColor { get; init; }

        [JsonPropertyName("user_flair_type")]
        public string? UserFlairType { get; init; }

        [JsonPropertyName("user_has_favorited")]
        public bool UserHasFavorited { get; init; }

        [JsonPropertyName("user_is_banned")]
        public bool UserIsBanned { get; init; }

        [JsonPropertyName("user_is_contributor")]
        public bool UserIsContributor { get; init; }

        [JsonPropertyName("user_is_moderator")]
        public bool UserIsModerator { get; init; }

        [JsonPropertyName("user_is_muted")]
        public bool UserIsMuted { get; init; }

        [JsonPropertyName("user_is_subscriber")]
        public bool UserIsSubscriber { get; set; }

        [JsonPropertyName("user_sr_flair_enabled")]
        public bool UserSrFlairEnabled { get; init; }

        [JsonPropertyName("user_sr_theme_enabled")]
        public bool UserSrThemeEnabled { get; init; }

        [JsonPropertyName("videostream_links_count")]
        public int VideoStreamLinksCount { get; init; }

        [JsonPropertyName("whitelist_status")]
        public string? WhitelistStatus { get; init; }

        [JsonPropertyName("wiki_enabled")]
        public object? WikiEnabled { get; init; }

        [JsonPropertyName("wls")]
        public int Wls { get; init; }
    }
}