using Deaddit.Attributes;
using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class RedditThing
    {
        [JsonPropertyName("all_awardings")]
        public List<object> AllAwardings { get; set; } = [];

        [JsonPropertyName("approved_at_utc")]
        public OptionalDateTime ApprovedAtUtc { get; set; }

        [JsonPropertyName("approved_by")]
        public object? ApprovedBy { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("author_flair_background_color")]
        public Color? AuthorFlairBackgroundColor { get; set; }

        [JsonPropertyName("author_flair_css_class")]
        public string? AuthorFlairCssClass { get; set; }

        [JsonPropertyName("author_flair_richtext")]
        public List<AuthorFlair> AuthorFlairRichText { get; set; } = [];

        [JsonPropertyName("author_flair_template_id")]
        public string? AuthorFlairTemplateId { get; set; }

        [JsonPropertyName("author_flair_text")]
        public string? AuthorFlairText { get; set; }

        [JsonPropertyName("author_flair_text_color")]
        public Color? AuthorFlairTextColor { get; set; }

        [JsonPropertyName("author_flair_type")]
        public string? AuthorFlairType { get; set; }

        [JsonPropertyName("author_fullname")]
        public string? AuthorFullName { get; set; }

        [JsonPropertyName("author_is_blocked")]
        public bool? AuthorIsBlocked { get; set; }

        [JsonPropertyName("author_patreon_flair")]
        public bool? AuthorPatreonFlair { get; set; }

        [JsonPropertyName("author_premium")]
        public bool? AuthorPremium { get; set; }

        [JsonPropertyName("awarders")]
        public List<object> Awarders { get; set; } = [];

        [JsonPropertyName("banned_at_utc")]
        public DateTime? BannedAtUtc { get; set; }

        [JsonPropertyName("banned_by")]
        public object? BannedBy { get; set; }

        [JsonPropertyNames("body", "selftext")]
        public string? Body { get; set; }

        [JsonPropertyNames("body_html", "selftext_html")]
        public string? BodyHtml { get; set; }

        [JsonPropertyName("can_gild")]
        public bool? CanGild { get; set; }

        [JsonPropertyName("can_mod_post")]
        public bool? CanModPost { get; set; }

        [JsonPropertyName("created_utc")]
        public OptionalDateTime CreatedUtc { get; set; }

        [JsonPropertyName("distinguished")]
        public DistinguishedKind Distinguished { get; set; }

        [JsonPropertyName("downs")]
        public long? Downs { get; set; }

        [JsonPropertyName("edited")]
        public OptionalDateTime Edited { get; set; }

        [JsonPropertyName("gilded")]
        public int? Gilded { get; set; }

        [JsonPropertyName("gildings")]
        public Gildings? Gildings { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("archived")]
        public bool? IsArchived { get; set; }

        [JsonPropertyName("locked")]
        public bool? IsLocked { get; set; }

        [JsonPropertyName("likes")]
        public UpvoteState Likes { get; set; }

        [JsonPropertyName("mod_note")]
        public object? ModNote { get; set; }

        [JsonPropertyName("mod_reason_by")]
        public object? ModReasonBy { get; set; }

        [JsonPropertyName("mod_reason_title")]
        public object? ModReasonTitle { get; set; }

        [JsonPropertyName("mod_reports")]
        public List<object> ModReports { get; set; } = [];

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("no_follow")]
        public bool? NoFollow { get; set; }

        [JsonPropertyName("num_reports")]
        public int? NumReports { get; set; }

        public RedditThing? Parent { get; set; }

        [JsonPropertyName("permalink")]
        public string? Permalink { get; set; }

        [JsonPropertyName("removal_reason")]
        public object? RemovalReason { get; set; }

        [JsonPropertyName("report_reasons")]
        public List<string> ReportReasons { get; set; } = [];

        [JsonPropertyName("saved")]
        public bool? Saved { get; set; }

        [JsonPropertyName("score")]
        public long? Score { get; set; }

        [JsonPropertyName("send_replies")]
        public bool? SendReplies { get; set; }

        [JsonPropertyName("stickied")]
        public bool? Stickied { get; set; }

        [JsonPropertyName("subreddit")]
        public string? SubReddit { get; set; }

        [JsonPropertyName("subreddit_id")]
        public string? SubRedditId { get; set; }

        [JsonPropertyName("subreddit_name_prefixed")]
        public string? SubredditNamePrefixed { get; set; }

        [JsonPropertyName("subreddit_type")]
        public string? SubredditType { get; set; }

        [JsonPropertyName("top_awarded_type")]
        public object? TopAwardedType { get; set; }

        [JsonPropertyName("total_awards_received")]
        public long? TotalAwardsReceived { get; set; }

        [JsonPropertyName("treatment_tags")]
        public List<object> TreatmentTags { get; set; } = [];

        [JsonPropertyName("ups")]
        public long? Ups { get; set; }

        [JsonPropertyName("user_reports")]
        public List<object> UserReports { get; set; } = [];
    }
}