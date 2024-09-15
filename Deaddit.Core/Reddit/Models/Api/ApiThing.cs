using Deaddit.Core.Json.Attributes;
using Deaddit.Core.Utils;
using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiThing
    {
        [JsonPropertyName("all_awardings")]
        public List<object> AllAwardings { get; init; } = [];

        [JsonPropertyName("approved_at_utc")]
        public OptionalDateTime ApprovedAtUtc { get; init; }

        [JsonPropertyName("author_cakeday")]
        public bool? AuthorCakeDay { get; init; }

        [JsonPropertyName("approved_by")]
        public object? ApprovedBy { get; init; }

        [JsonPropertyName("author")]
        public string? Author { get; init; }

        [JsonPropertyName("author_flair_background_color")]
        public DynamicColor? AuthorFlairBackgroundColor { get; init; }

        [JsonPropertyName("author_flair_css_class")]
        public string? AuthorFlairCssClass { get; init; }

        [JsonPropertyName("author_flair_richtext")]
        public List<AuthorFlair> AuthorFlairRichText { get; init; } = [];

        [JsonPropertyName("author_flair_template_id")]
        public string? AuthorFlairTemplateId { get; init; }

        [JsonPropertyName("author_flair_text")]
        public string? AuthorFlairText { get; init; }

        [JsonPropertyName("author_flair_text_color")]
        public DynamicColor? AuthorFlairTextColor { get; init; }

        [JsonPropertyName("author_flair_type")]
        public string? AuthorFlairType { get; init; }

        [JsonPropertyName("author_fullname")]
        public string? AuthorFullName { get; init; }

        [JsonPropertyName("author_is_blocked")]
        public bool? AuthorIsBlocked { get; init; }

        [JsonPropertyName("author_patreon_flair")]
        public bool? AuthorPatreonFlair { get; init; }

        [JsonPropertyName("author_premium")]
        public bool? AuthorPremium { get; init; }

        [JsonPropertyName("awarders")]
        public List<object> Awarders { get; init; } = [];

        [JsonPropertyName("banned_at_utc")]
        public DateTime? BannedAtUtc { get; init; }

        [JsonPropertyName("banned_by")]
        public object? BannedBy { get; init; }

        [JsonPropertyNames("body", "selftext")]
        public string? Body { get; set; }

        [JsonPropertyNames("body_html", "selftext_html")]
        public string? BodyHtml { get; init; }

        [JsonPropertyName("can_gild")]
        public bool? CanGild { get; init; }

        [JsonPropertyName("can_mod_post")]
        public bool? CanModPost { get; init; }

        [JsonPropertyName("created")]
        [Obsolete("Use the UTC version", true)]
        public OptionalDateTime Created { get; init; }

        [JsonPropertyName("created_utc")]
        public OptionalDateTime CreatedUtc { get; init; }

        [JsonPropertyName("distinguished")]
        public DistinguishedKind Distinguished { get; init; }

        [JsonPropertyName("downs")]
        public long? Downs { get; init; }

        [JsonPropertyName("edited")]
        public OptionalDateTime Edited { get; init; }

        [JsonPropertyName("gilded")]
        public int? Gilded { get; init; }

        [JsonPropertyName("gildings")]
        public Gildings? Gildings { get; init; }

        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("archived")]
        public bool? IsArchived { get; init; }

        [JsonPropertyName("locked")]
        public bool? IsLocked { get; init; }

        [JsonPropertyName("likes")]
        public UpvoteState Likes { get; set; }

        [JsonPropertyName("media_metadata")]
        public Dictionary<string, MediaMetaData>? MediaMetaData { get; init; } = [];

        [JsonPropertyName("mod_note")]
        public object? ModNote { get; init; }

        [JsonPropertyName("mod_reason_by")]
        public object? ModReasonBy { get; init; }

        [JsonPropertyName("mod_reason_title")]
        public object? ModReasonTitle { get; init; }

        [JsonPropertyName("mod_reports")]
        public List<object> ModReports { get; init; } = [];

        [JsonPropertyName("name")]
        public string Name { get; init; }

        [JsonPropertyName("no_follow")]
        public bool? NoFollow { get; init; }

        [JsonPropertyName("num_reports")]
        public int? NumReports { get; init; }

        public ApiThing? Parent { get; set; }

        [JsonPropertyName("permalink")]
        public string? Permalink { get; init; }

        [JsonPropertyName("removal_reason")]
        public RemovalReason RemovalReason { get; init; }

        [JsonPropertyName("report_reasons")]
        public List<string> ReportReasons { get; init; } = [];

        [JsonPropertyName("saved")]
        public bool? Saved { get; set; }

        [JsonPropertyName("score")]
        public long? Score { get; set; }

        [JsonPropertyName("send_replies")]
        public bool? SendReplies { get; set; }

        [JsonPropertyName("stickied")]
        public bool? Stickied { get; init; }

        [JsonPropertyName("subreddit")]
        public string? SubReddit { get; init; }

        [JsonPropertyName("subreddit_id")]
        public string? SubRedditId { get; init; }

        [JsonPropertyName("subreddit_name_prefixed")]
        public string? SubredditNamePrefixed { get; init; }

        [JsonPropertyName("subreddit_type")]
        public string? SubredditType { get; init; }

        [JsonPropertyName("top_awarded_type")]
        public object? TopAwardedType { get; init; }

        [JsonPropertyName("total_awards_received")]
        public long? TotalAwardsReceived { get; init; }

        [JsonPropertyName("treatment_tags")]
        public List<object> TreatmentTags { get; init; } = [];

        [JsonPropertyName("ups")]
        public long? Ups { get; init; }

        [JsonPropertyName("user_reports")]
        public List<object> UserReports { get; init; } = [];
    }
}