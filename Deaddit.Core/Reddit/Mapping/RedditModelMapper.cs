using Deaddit.Core.Reddit.Models.Api;
using Reddit.Api.Models.Json.Common;
using Reddit.Api.Models.Json.Listings;

namespace Deaddit.Core.Reddit.Mapping
{
    /// <summary>
    /// Maps between Reddit API JSON models and the Api models used by the application.
    /// </summary>
    public static class RedditModelMapper
    {
        #region Link/Post Mapping

        public static ApiPost Map(Link source)
        {
            if (source == null)
            {
                return null!;
            }

            return new ApiPost
            {
                Id = source.Id,
                Name = source.Name,
                Title = source.Title,
                Author = source.Author,
                AuthorFullName = source.AuthorFullname,
                AuthorFlairText = source.AuthorFlairText,
                AuthorFlairCssClass = source.AuthorFlairCssClass,
                AuthorFlairBackgroundColor = source.AuthorFlairBackgroundColor,
                AuthorFlairTextColor = source.AuthorFlairTextColor,
                AuthorFlairTemplateId = source.AuthorFlairTemplateId,
                Body = source.Selftext ?? string.Empty,
                BodyHtml = source.SelftextHtml ?? string.Empty,
                Url = source.Url,
                UrlOverriddenByDest = source.UrlOverriddenByDest,
                Domain = source.Domain,
                SubRedditName = source.Subreddit,
                SubRedditId = source.SubredditId,
                SubredditNamePrefixed = source.SubredditNamePrefixed,
                SubredditType = source.SubredditType,
                SubredditSubscribers = source.SubredditSubscribers ?? 0,
                Score = source.Score,
                Ups = source.Ups,
                Downs = source.Downs,
                UpvoteRatio = source.UpvoteRatio,
                NumComments = source.NumComments,
                CreatedUtc = source.CreatedUtc,
                Edited = source.Edited,
                Permalink = source.Permalink,
                IsSelf = source.IsSelf,
                IsVideo = source.IsVideo,
                IsOriginalContent = source.IsOriginalContent,
                IsRedditMediaDomain = source.IsRedditMediaDomain,
                IsMeta = source.IsMeta,
                IsCrossPostable = source.IsCrosspostable,
                IsRobotIndexable = source.IsRobotIndexable,
                IsNsfw = source.Over18,
                Spoiler = source.Spoiler,
                IsLocked = source.Locked,
                Stickied = source.Stickied,
                IsArchived = source.Archived,
                Hidden = source.Hidden,
                Saved = source.Saved,
                Clicked = source.Clicked,
                Visited = source.Visited,
                Likes = source.Likes,
                HideScore = source.HideScore,
                ContestMode = source.ContestMode,
                Pinned = source.Pinned,
                Quarantine = source.Quarantine,
                SendReplies = source.SendReplies,
                CanModPost = source.CanModPost,
                CanGild = source.CanGild,
                NoFollow = source.NoFollow,
                Distinguished = source.Distinguished,
                LinkFlairText = source.LinkFlairText,
                LinkFlairCssClass = source.LinkFlairCssClass,
                LinkFlairBackgroundColor = source.LinkFlairBackgroundColor,
                LinkFlairTextColor = source.LinkFlairTextColor,
                LinkFlairTemplateId = source.LinkFlairTemplateId,
                LinkFlairType = source.LinkFlairType,
                Thumbnail = source.Thumbnail,
                ThumbnailHeight = source.ThumbnailHeight,
                ThumbnailWidth = source.ThumbnailWidth,
                PostHint = source.PostHint,
                CrossPostParent = source.CrosspostParent,
                NumCrossPosts = source.NumCrossposts,
                Gilded = source.Gilded,
                TotalAwardsReceived = source.TotalAwardsReceived,
                NumReports = source.NumReports,
                ModNote = source.ModNote,
                ModReasonBy = source.ModReasonBy,
                ModReasonTitle = source.ModReasonTitle,
                ApprovedAtUtc = source.ApprovedAtUtc,
                ApprovedBy = source.ApprovedBy,
                BannedAtUtc = source.BannedAtUtc,
                BannedBy = source.BannedBy,
                RemovedBy = source.RemovedBy,
                RemovedByCategory = source.RemovedByCategory,
                SuggestedSort = source.SuggestedSort,
                ViewCount = source.ViewCount,
                Wls = source.Wls,
                Pwls = source.Pwls,
                AllowLiveComments = source.AllowLiveComments,
                ContentCategories = source.ContentCategories ?? [],
                IsGallery = source.IsGallery
            };
        }

        public static ApiPost Map(Thing<Link> source)
        {
            if (source?.Data == null)
            {
                return null!;
            }

            return Map(source.Data);
        }

        #endregion Link/Post Mapping

        #region Comment Mapping

        public static ApiComment Map(Comment source)
        {
            if (source == null)
            {
                return null!;
            }

            return new ApiComment
            {
                Id = source.Id,
                Name = source.Name,
                Author = source.Author,
                AuthorFullName = source.AuthorFullname,
                AuthorFlairText = source.AuthorFlairText,
                AuthorFlairCssClass = source.AuthorFlairCssClass,
                AuthorFlairBackgroundColor = source.AuthorFlairBackgroundColor,
                AuthorFlairTextColor = source.AuthorFlairTextColor,
                AuthorFlairTemplateId = source.AuthorFlairTemplateId,
                Body = source.Body,
                BodyHtml = source.BodyHtml ?? string.Empty,
                LinkId = source.LinkId,
                ParentId = source.ParentId,
                LinkTitle = source.LinkTitle,
                Linkauthor = source.LinkAuthor,
                LinkPermalink = source.LinkPermalink,
                LinkUrl = source.LinkUrl,
                SubRedditName = source.Subreddit,
                SubRedditId = source.SubredditId,
                SubredditNamePrefixed = source.SubredditNamePrefixed,
                SubredditType = source.SubredditType,
                Score = source.Score,
                Ups = source.Ups,
                Downs = source.Downs,
                CreatedUtc = source.CreatedUtc,
                Edited = source.Edited,
                Permalink = source.Permalink,
                IsLocked = source.Locked,
                Stickied = source.Stickied,
                IsArchived = source.Archived,
                Saved = source.Saved,
                Likes = source.Likes,
                ScoreHidden = source.ScoreHidden,
                IsSubmitter = source.IsSubmitter,
                CanModPost = source.CanModPost,
                CanGild = source.CanGild,
                NoFollow = source.NoFollow,
                SendReplies = source.SendReplies,
                Distinguished = source.Distinguished,
                Collapsed = source.Collapsed,
                CollapsedReason = source.CollapsedReason,
                CollapsedReasonCode = source.CollapsedReasonCode,
                Controversiality = source.Controversiality,
                Depth = source.Depth,
                Gilded = source.Gilded,
                TotalAwardsReceived = source.TotalAwardsReceived,
                NumReports = source.NumReports,
                ModNote = source.ModNote,
                ModReasonBy = source.ModReasonBy,
                ModReasonTitle = source.ModReasonTitle,
                ApprovedAtUtc = source.ApprovedAtUtc,
                ApprovedBy = source.ApprovedBy,
                BannedAtUtc = source.BannedAtUtc,
                BannedBy = source.BannedBy
            };
        }

        public static ApiComment Map(Thing<Comment> source)
        {
            if (source?.Data == null)
            {
                return null!;
            }

            return Map(source.Data);
        }

        #endregion Comment Mapping

        #region More Mapping

        public static ApiMore MapMore(Comment source)
        {
            if (source == null)
            {
                return null!;
            }

            return new ApiMore
            {
                Id = source.Id,
                Name = source.Name,
                ParentId = source.ParentId,
                ChildNames = source.Children ?? [],
                Count = source.Count,
                Depth = source.Depth
            };
        }

        #endregion More Mapping

        #region Collection Mappings

        public static List<ApiThing> MapComments(Listing<Thing<Comment>>? listing)
        {
            if (listing?.Data?.Children == null)
            {
                return [];
            }

            return listing.Data.Children
                .Where(c => c?.Data != null)
                .Select(c => (ApiThing)Map(c.Data!))
                .ToList();
        }

        public static List<ApiThing> MapPosts(Listing<Thing<Link>>? listing)
        {
            if (listing?.Data?.Children == null)
            {
                return [];
            }

            return listing.Data.Children
                .Where(c => c?.Data != null)
                .Select(c => (ApiThing)Map(c.Data!))
                .ToList();
        }

        public static List<ApiPost> MapPostsTyped(Listing<Thing<Link>>? listing)
        {
            if (listing?.Data?.Children == null)
            {
                return [];
            }

            return listing.Data.Children
                .Where(c => c?.Data != null)
                .Select(c => Map(c.Data!))
                .ToList();
        }

        #endregion Collection Mappings
    }
}