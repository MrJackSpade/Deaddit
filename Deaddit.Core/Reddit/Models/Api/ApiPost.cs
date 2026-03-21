using Reddit.Api.Models;
using Reddit.Api.Models.Enums;
using Reddit.Api.Models.Json.Listings;
using System.Diagnostics;

namespace Deaddit.Core.Reddit.Models.Api
{
    [DebuggerDisplay("{Title}")]
    public class ApiPost : ApiThing
    {
        public bool AllowLiveComments { get; init; }

        public string? Category { get; init; }

        public bool Clicked { get; init; }

        public List<string> ContentCategories { get; init; } = [];

        public bool ContestMode { get; init; }

        public string? CrossPostParent { get; init; }

        public List<ApiPost> CrossPostParentList { get; init; } = [];

        public object? DiscussionType { get; init; }

        public string? Domain { get; init; }

        public GalleryData? GalleryData { get; init; }

        public bool Hidden { get; init; }

        public bool HideScore { get; init; }

        public bool IsCreatedFromAdsUi { get; init; }

        public bool IsCrossPostable { get; init; }

        public bool? IsGallery { get; init; }

        public bool IsMeta { get; init; }

        public bool IsNsfw { get; init; }

        public bool IsOriginalContent { get; init; }

        public bool IsRedditMediaDomain { get; init; }

        public bool IsRobotIndexable { get; init; }

        public bool IsSelf { get; init; }

        public bool IsVideo { get; init; }

        public JsonColor LinkFlairBackgroundColor { get; init; }

        public string? LinkFlairCssClass { get; init; }

        public List<FlairRichtext> LinkFlairRichText { get; init; } = [];

        public string? LinkFlairTemplateId { get; init; }

        public string? LinkFlairText { get; init; }

        public FlairTextColor LinkFlairTextColor { get; init; }

        public string? LinkFlairType { get; init; }

        public Media? Media { get; init; }

        public Media? MediaEmbed { get; init; }

        public bool MediaOnly { get; init; }

        public int? NumberOfDuplicates { get; init; }

        public long NumCrossPosts { get; init; }

        public string? ParentWhitelistStatus { get; init; }

        public bool Pinned { get; init; }

        public PostHint PostHint { get; init; }

        public Preview? Preview { get; init; }

        public long? Pwls { get; init; }

        public bool Quarantine { get; init; }

        public string? RemovedBy { get; init; }

        public string? RemovedByCategory { get; init; }

        public Media? SecureMedia { get; init; }

        public Media? SecureMediaEmbed { get; init; }

        public bool Spoiler { get; init; }

        public long SubredditSubscribers { get; init; }

        public CommentSort? SuggestedSort { get; init; }

        public string? Thumbnail { get; init; }

        public int? ThumbnailHeight { get; init; }

        public int? ThumbnailWidth { get; init; }

        public string? Title { get; init; }

        public double UpvoteRatio { get; init; }

        public string? Url { get; init; }

        public string? UrlOverriddenByDest { get; init; }

        public int? ViewCount { get; init; }

        public bool Visited { get; init; }

        public string? WhitelistStatus { get; init; }

        public long? Wls { get; init; }
    }
}