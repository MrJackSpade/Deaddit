using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class ApiSubReddit : ApiThing
    {
        [JsonPropertyName("user_flair_background_color")]
        public object? UserFlairBackgroundColor { get; set; }

        [JsonPropertyName("submit_text_html")]
        public object? SubmitTextHtml { get; set; }

        [JsonPropertyName("restrict_posting")]
        public bool RestrictPosting { get; set; }

        [JsonPropertyName("user_is_banned")]
        public bool UserIsBanned { get; set; }

        [JsonPropertyName("free_form_reports")]
        public bool FreeFormReports { get; set; }

        [JsonPropertyName("wiki_enabled")]
        public object? WikiEnabled { get; set; }

        [JsonPropertyName("user_is_muted")]
        public bool UserIsMuted { get; set; }

        [JsonPropertyName("user_can_flair_in_sr")]
        public bool UserCanFlairInSr { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("header_img")]
        public object? HeaderImg { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("allow_galleries")]
        public bool AllowGalleries { get; set; }

        [JsonPropertyName("icon_size")]
        public object? IconSize { get; set; }

        [JsonPropertyName("primary_color")]
        public string? PrimaryColor { get; set; }

        [JsonPropertyName("active_user_count")]
        public int ActiveUserCount { get; set; }

        [JsonPropertyName("icon_img")]
        public string? IconImg { get; set; }

        [JsonPropertyName("display_name_prefixed")]
        public string? DisplayNamePrefixed { get; set; }

        [JsonPropertyName("accounts_active")]
        public int AccountsActive { get; set; }

        [JsonPropertyName("public_traffic")]
        public bool PublicTraffic { get; set; }

        [JsonPropertyName("subscribers")]
        public int Subscribers { get; set; }

        [JsonPropertyName("user_flair_richtext")]
        public List<object> UserFlairRichtext { get; set; } = [];

        [JsonPropertyName("videostream_links_count")]
        public int VideoStreamLinksCount { get; set; }

        [JsonPropertyName("quarantine")]
        public bool Quarantine { get; set; }

        [JsonPropertyName("hide_ads")]
        public bool HideAds { get; set; }

        [JsonPropertyName("prediction_leaderboard_entry_type")]
        public int PredictionLeaderboardEntryType { get; set; }

        [JsonPropertyName("emojis_enabled")]
        public bool EmojisEnabled { get; set; }

        [JsonPropertyName("advertiser_category")]
        public string? AdvertiserCategory { get; set; }

        [JsonPropertyName("public_description")]
        public string? PublicDescription { get; set; }

        [JsonPropertyName("comment_score_hide_mins")]
        public int CommentScoreHideMins { get; set; }

        [JsonPropertyName("allow_predictions")]
        public bool AllowPredictions { get; set; }

        [JsonPropertyName("user_has_favorited")]
        public bool UserHasFavorited { get; set; }

        [JsonPropertyName("user_flair_template_id")]
        public object? UserFlairTemplateId { get; set; }

        [JsonPropertyName("community_icon")]
        public string? CommunityIcon { get; set; }

        [JsonPropertyName("banner_background_image")]
        public string? BannerBackgroundImage { get; set; }

        [JsonPropertyName("original_content_tag_enabled")]
        public bool OriginalContentTagEnabled { get; set; }

        [JsonPropertyName("community_reviewed")]
        public bool CommunityReviewed { get; set; }

        [JsonPropertyName("submit_text")]
        public string? SubmitText { get; set; }

        [JsonPropertyName("description_html")]
        public string? DescriptionHtml { get; set; }

        [JsonPropertyName("spoilers_enabled")]
        public bool SpoilersEnabled { get; set; }

        [JsonPropertyName("comment_contribution_settings")]
        public CommentContributionSettings? CommentContributionSettings { get; set; }

        [JsonPropertyName("allow_talks")]
        public bool AllowTalks { get; set; }

        [JsonPropertyName("header_size")]
        public object? HeaderSize { get; set; }

        [JsonPropertyName("user_flair_position")]
        public string? UserFlairPosition { get; set; }

        [JsonPropertyName("all_original_content")]
        public bool AllOriginalContent { get; set; }

        [JsonPropertyName("has_menu_widget")]
        public bool HasMenuWidget { get; set; }

        [JsonPropertyName("is_enrolled_in_new_modmail")]
        public bool? IsEnrolledInNewModmail { get; set; }

        [JsonPropertyName("key_color")]
        public Color? KeyColor { get; set; }

        [JsonPropertyName("can_assign_user_flair")]
        public bool CanAssignUserFlair { get; set; }

        [JsonPropertyName("wls")]
        public int Wls { get; set; }

        [JsonPropertyName("show_media_preview")]
        public bool ShowMediaPreview { get; set; }

        [JsonPropertyName("submission_type")]
        public string? SubmissionType { get; set; }

        [JsonPropertyName("user_is_subscriber")]
        public bool UserIsSubscriber { get; set; }

        [JsonPropertyName("allowed_media_in_comments")]
        public List<string> AllowedMediaInComments { get; set; } = [];

        [JsonPropertyName("allow_videogifs")]
        public bool AllowVideogifs { get; set; }

        [JsonPropertyName("should_archive_posts")]
        public bool ShouldArchivePosts { get; set; }

        [JsonPropertyName("user_flair_type")]
        public string? UserFlairType { get; set; }

        [JsonPropertyName("allow_polls")]
        public bool AllowPolls { get; set; }

        [JsonPropertyName("collapse_deleted_comments")]
        public bool CollapseDeletedComments { get; set; }

        [JsonPropertyName("emojis_custom_size")]
        public object? EmojisCustomSize { get; set; }

        [JsonPropertyName("public_description_html")]
        public string? PublicDescriptionHtml { get; set; }

        [JsonPropertyName("allow_videos")]
        public bool AllowVideos { get; set; }

        [JsonPropertyName("is_crosspostable_subreddit")]
        public object? IsCrosspostableSubreddit { get; set; }

        [JsonPropertyName("notification_level")]
        public object? NotificationLevel { get; set; }

        [JsonPropertyName("should_show_media_in_comments_setting")]
        public bool ShouldShowMediaInCommentsSetting { get; set; }

        [JsonPropertyName("can_assign_link_flair")]
        public bool CanAssignLinkFlair { get; set; }

        [JsonPropertyName("accounts_active_is_fuzzed")]
        public bool AccountsActiveIsFuzzed { get; set; }

        [JsonPropertyName("allow_prediction_contributors")]
        public bool AllowPredictionContributors { get; set; }

        [JsonPropertyName("submit_text_label")]
        public string? SubmitTextLabel { get; set; }

        [JsonPropertyName("link_flair_position")]
        public string? LinkFlairPosition { get; set; }

        [JsonPropertyName("user_sr_flair_enabled")]
        public bool UserSrFlairEnabled { get; set; }

        [JsonPropertyName("user_flair_enabled_in_sr")]
        public bool UserFlairEnabledInSr { get; set; }

        [JsonPropertyName("allow_discovery")]
        public bool AllowDiscovery { get; set; }

        [JsonPropertyName("accept_followers")]
        public bool AcceptFollowers { get; set; }

        [JsonPropertyName("user_sr_theme_enabled")]
        public bool UserSrThemeEnabled { get; set; }

        [JsonPropertyName("link_flair_enabled")]
        public bool LinkFlairEnabled { get; set; }

        [JsonPropertyName("disable_contributor_requests")]
        public bool DisableContributorRequests { get; set; }

        [JsonPropertyName("suggested_comment_sort")]
        public string? SuggestedCommentSort { get; set; }

        [JsonPropertyName("banner_img")]
        public string? BannerImg { get; set; }

        [JsonPropertyName("user_flair_text")]
        public object? UserFlairText { get; set; }

        [JsonPropertyName("banner_background_color")]
        public string? BannerBackgroundColor { get; set; }

        [JsonPropertyName("show_media")]
        public bool ShowMedia { get; set; }

        [JsonPropertyName("user_is_moderator")]
        public bool UserIsModerator { get; set; }

        [JsonPropertyName("over18")]
        public bool IsNSFW { get; set; }

        [JsonPropertyName("header_title")]
        public string? HeaderTitle { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("submit_link_label")]
        public string? SubmitLinkLabel { get; set; }

        [JsonPropertyName("user_flair_text_color")]
        public Color? UserFlairTextColor { get; set; }

        [JsonPropertyName("restrict_commenting")]
        public bool RestrictCommenting { get; set; }

        [JsonPropertyName("user_flair_css_class")]
        public string? UserFlairCssClass { get; set; }

        [JsonPropertyName("allow_images")]
        public bool AllowImages { get; set; }

        [JsonPropertyName("lang")]
        public string? Lang { get; set; }

        [JsonPropertyName("whitelist_status")]
        public string? WhitelistStatus { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("banner_size")]
        public object? BannerSize { get; set; }

        [JsonPropertyName("mobile_banner_image")]
        public string? MobileBannerImage { get; set; }

        [JsonPropertyName("user_is_contributor")]
        public bool UserIsContributor { get; set; }

        [JsonPropertyName("allow_predictions_tournament")]
        public bool AllowPredictionsTournament { get; set; }
    }
}
