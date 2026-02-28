using Deaddit.Core.Reddit.Models.Api;
using Reddit.Api.Models.Json.Common;
using Reddit.Api.Models.Json.Listings;
using Reddit.Api.Models.Json.Multis;
using Reddit.Api.Models.Json.Subreddits;
using Reddit.Api.Models.Json.Users;

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

        #region Subreddit Mapping

        public static ApiSubReddit Map(Subreddit source)
        {
            if (source == null)
            {
                return null!;
            }

            return new ApiSubReddit
            {
                Id = source.Id,
                Name = source.Name,
                DisplayName = source.DisplayName,
                DisplayNamePrefixed = source.DisplayNamePrefixed,
                Title = source.Title,
                PublicDescription = source.PublicDescription,
                PublicDescriptionHtml = source.PublicDescriptionHtml,
                Description = source.Description,
                DescriptionHtml = source.DescriptionHtml,
                SubmitText = source.SubmitText,
                SubmitTextHtml = source.SubmitTextHtml,
                SubredditType = source.SubredditType,
                SubmissionType = source.SubmissionType,
                Subscribers = source.Subscribers,
                ActiveUserCount = source.ActiveUserCount,
                AccountsActive = source.AccountsActive,
                CreatedUtc = source.CreatedUtc,
                IsNSFW = source.Over18,
                UserIsSubscriber = source.UserIsSubscriber,
                UserIsModerator = source.UserIsModerator,
                UserIsContributor = source.UserIsContributor,
                UserIsBanned = source.UserIsBanned,
                UserIsMuted = source.UserIsMuted,
                UserHasFavorited = source.UserHasFavorited,
                UserFlairEnabledInSr = source.UserFlairEnabledInSr,
                UserFlairBackgroundColor = source.UserFlairBackgroundColor,
                UserFlairCssClass = source.UserFlairCssClass,
                UserFlairPosition = source.UserFlairPosition,
                UserFlairText = source.UserFlairText,
                UserFlairTextColor = source.UserFlairTextColor,
                UserFlairType = source.UserFlairType,
                CanAssignLinkFlair = source.CanAssignLinkFlair,
                CanAssignUserFlair = source.CanAssignUserFlair,
                LinkFlairEnabled = source.LinkFlairEnabled,
                LinkFlairPosition = source.LinkFlairPosition,
                SpoilersEnabled = source.SpilersEnabled,
                OriginalContentTagEnabled = source.OriginalContentTagEnabled,
                AllowImages = source.AllowImages,
                AllowVideos = source.AllowVideos,
                AllowVideoGifs = source.AllowVideogifs,
                AllowGalleries = source.AllowGalleries,
                AllowPolls = source.AllowPolls,
                AllowPredictions = source.AllowPredictions,
                AllowPredictionsTournament = source.AllowPredictionsTournament,
                AllowTalks = source.AllowTalks,
                CommunityIcon = source.CommunityIcon,
                IconImg = source.IconImg,
                IconSize = source.IconSize ?? [],
                HeaderImg = source.HeaderImg,
                HeaderSize = source.HeaderSize?.ToArray() ?? [],
                HeaderTitle = source.HeaderTitle,
                BannerImg = source.BannerImg,
                BannerSize = source.BannerSize?.ToArray() ?? [],
                BannerBackgroundImage = source.BannerBackgroundImage,
                BannerBackgroundColor = source.BannerBackgroundColor,
                MobileBannerImage = source.MobileBannerImage,
                PrimaryColor = source.PrimaryColor,
                KeyColor = source.KeyColor,
                Url = source.Url,
                Lang = source.Lang,
                WikiEnabled = source.WikiEnabled,
                ShowMedia = source.ShowMedia,
                ShowMediaPreview = source.ShowMediaPreview,
                HideAds = source.HideAds,
                EmojisEnabled = source.EmojisEnabled,
                EmojisCustomSize = source.EmojisCustomSize?.ToArray() ?? [],
                AdvertiserCategory = source.AdvertiserCategory,
                WhitelistStatus = source.WhitelistStatus,
                Wls = source.Wls,
                FreeFormReports = source.FreeFormReports,
                Quarantine = source.Quarantine,
                IsCrosspostableSubreddit = source.IsCrosspostableSubreddit,
                NotificationLevel = source.NotificationLevel,
                RestrictCommenting = source.RestrictCommenting,
                RestrictPosting = source.RestrictPosting,
                CommentScoreHideMins = source.CommentScoreHideMins,
                SuggestedCommentSort = source.SuggestedCommentSort,
                AcceptFollowers = source.AcceptFollowers,
                DisableContributorRequests = source.DisableContributorRequests,
                CollapseDeletedComments = source.CollapseDeletedComments,
                PublicTraffic = source.PublicTraffic
            };
        }

        public static ApiSubReddit Map(Thing<Subreddit> source)
        {
            if (source?.Data == null)
            {
                return null!;
            }

            return Map(source.Data);
        }

        #endregion Subreddit Mapping

        #region User Mapping

        public static ApiUser Map(User source)
        {
            if (source == null)
            {
                return null!;
            }

            return new ApiUser
            {
                Id = source.Id,
                Name = source.Name,
                CreatedUtc = source.CreatedUtc,
                LinkKarma = source.LinkKarma,
                CommentKarma = source.CommentKarma,
                TotalKarma = source.TotalKarma ?? 0,
                AwardeeKarma = source.AwardeeKarma ?? 0,
                AwarderKarma = source.AwarderKarma ?? 0,
                IsGold = source.IsGold,
                IsMod = source.IsMod,
                IsEmployee = source.IsEmployee,
                Verified = source.Verified,
                HasVerifiedEmail = source.HasVerifiedEmail,
                IconImg = source.IconImg,
                SnoovatarImg = source.SnoovatarImg,
                SnoovatarSize = source.SnoovatarSize ?? [],
                HideFromRobots = source.HideFromRobots,
                PrefShowSnoovatar = source.PrefShowSnoovatar,
                AcceptChats = source.AcceptChats,
                AcceptPms = source.AcceptPms,
                AcceptFollowers = source.AcceptFollowers,
                IsBlocked = source.IsBlocked,
                IsFriend = source.IsFriend,
                HasSubscribed = source.HasSubscribed
            };
        }

        public static ApiUser Map(Thing<User> source)
        {
            if (source?.Data == null)
            {
                return null!;
            }

            return Map(source.Data);
        }

        #endregion User Mapping

        #region Multi Mapping

        public static ApiMulti Map(MultiResponse source)
        {
            if (source?.Data == null)
            {
                return null!;
            }

            return Map(source.Data);
        }

        public static ApiMulti Map(Multi source)
        {
            if (source == null)
            {
                return null!;
            }

            return new ApiMulti
            {
                Name = source.Name,
                DisplayName = source.DisplayName,
                Path = source.Path,
                DescriptionMd = source.DescriptionMd,
                DescriptionHtml = source.DescriptionHtml,
                IconUrl = source.IconUrl,
                Visibility = source.Visibility,
                Over18 = source.Over18,
                Owner = source.Owner,
                OwnerId = source.OwnerId,
                CanEdit = source.CanEdit,
                IsSubscriber = source.IsSubscriber,
                IsFavorited = source.IsFavorited,
                NumSubscribers = source.NumSubscribers,
                CopiedFrom = source.CopiedFrom,
                Created = source.Created,
                CreatedUtc = source.CreatedUtc,
                Subreddits = source.Subreddits?.Select(s => new ApiMultiSubReddit { Name = s.Name }).ToList() ?? []
            };
        }

        public static List<ApiMulti> Map(List<MultiResponse> source)
        {
            if (source == null)
            {
                return [];
            }

            return source.Select(Map).Where(m => m != null).ToList();
        }

        #endregion Multi Mapping

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