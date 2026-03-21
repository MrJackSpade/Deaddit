using Reddit.Api.Models;
using Reddit.Api.Models.Enums;
using Reddit.Api.Models.Json.Listings;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiThing
    {
        public List<object> AllAwardings { get; init; } = [];

        public JsonDateTime ApprovedAtUtc { get; init; }

        public string? ApprovedBy { get; init; }

        public string? Author { get; init; }

        public bool? AuthorCakeDay { get; init; }

        public JsonColor AuthorFlairBackgroundColor { get; init; }

        public string? AuthorFlairCssClass { get; init; }

        public List<FlairRichtext> AuthorFlairRichText { get; init; } = [];

        public string? AuthorFlairTemplateId { get; init; }

        public string? AuthorFlairText { get; init; }

        public FlairTextColor AuthorFlairTextColor { get; init; }

        public string? AuthorFlairType { get; init; }

        public string? AuthorFullName { get; init; }

        public bool? AuthorIsBlocked { get; init; }

        public bool? AuthorPatreonFlair { get; init; }

        public bool? AuthorPremium { get; init; }

        public List<object> Awarders { get; init; } = [];

        public DateTime? BannedAtUtc { get; init; }

        public string? BannedBy { get; init; }

        public string Body { get; set; } = string.Empty;

        public string BodyHtml { get; set; } = string.Empty;

        public bool? CanGild { get; init; }

        public bool? CanModPost { get; init; }

        [Obsolete("Use the UTC version", true)]
        public JsonDateTime Created { get; init; }

        public JsonDateTime CreatedUtc { get; init; }

        public DistinguishedKind Distinguished { get; init; }

        public long? Downs { get; init; }

        public JsonDateTime Edited { get; init; }

        public int? Gilded { get; init; }

        public Gildings? Gildings { get; init; }

        public string? Id { get; init; }

        public bool? IsArchived { get; init; }

        public bool? IsLocked { get; init; }

        public VoteState Likes { get; set; }

        public Dictionary<string, MediaMetadata>? MediaMetaData { get; init; } = [];

        public string? ModNote { get; init; }

        public string? ModReasonBy { get; init; }

        public string? ModReasonTitle { get; init; }

        public List<object> ModReports { get; init; } = [];

        public string Name { get; init; }

        public bool? NoFollow { get; init; }

        public int? NumComments { get; set; }

        public int? NumReports { get; init; }

        public ApiThing? Parent { get; set; }

        public string? ParentId { get; set; }

        public string? Permalink { get; init; }

        public RemovalReason RemovalReason { get; init; }

        public List<string> ReportReasons { get; init; } = [];

        public bool? Saved { get; set; }

        public long? Score { get; set; }

        public bool? SendReplies { get; set; }

        public bool? Stickied { get; init; }

        public string? SubRedditId { get; init; }

        public string? SubRedditName { get; set; }

        public string? SubredditNamePrefixed { get; init; }

        public SubredditType SubredditType { get; init; }

        public object? TopAwardedType { get; init; }

        public long? TotalAwardsReceived { get; init; }

        public List<object> TreatmentTags { get; init; } = [];

        public long? Ups { get; init; }

        public List<object> UserReports { get; init; } = [];
    }
}