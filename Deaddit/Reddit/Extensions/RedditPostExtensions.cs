using Deaddit.Reddit.Exceptions;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;
using System.Web;

namespace Deaddit.Reddit.Extensions
{
    public static class RedditPostExtensions
    {
        public static PostTarget GetResource(this RedditPost post)
        {
            if (post.IsSelf)
            {
                return new PostTarget(PostTargetKind.Post, null);
            }

            if (post.Media is not null)
            {
                if (post.Media.RedditVideo is null)
                {
                    throw new InvalidApiResponseException("Post is media type but RedditVideo property is null");
                }

                return new PostTarget(PostTargetKind.Video, post.Media.RedditVideo.DashUrl);
            }

            Ensure.NotNullOrWhiteSpace(post.Url);

            return UrlHandler.Resolve(post.Url);
        }

        public static string? TryGetPreview(this RedditPost redditPost)
        {
            if (redditPost?.Thumbnail?.Contains("://") ?? false)
            {
                return HttpUtility.HtmlDecode(redditPost?.Thumbnail);
            }

            if (redditPost?.Preview?.Images is null ||
                redditPost.Preview.Images.Count == 0)
            {
                return null;
            }

            return HttpUtility.HtmlDecode(redditPost.Preview.Images[0].Source?.Url);
        }
    }
}