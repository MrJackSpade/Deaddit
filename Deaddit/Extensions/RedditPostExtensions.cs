using Deaddit.Models;
using Deaddit.Models.Json.Response;
using Deaddit.Services;
using System.Web;

namespace Deaddit.Extensions
{
    public static class RedditPostExtensions
    {
        public static RedditResource GetResource(this RedditPost post)
        {
            if (post.IsSelf)
            {
                return new RedditResource(RedditResourceKind.Post, null);
            }

            if (post.Media is not null)
            {
                return new RedditResource(RedditResourceKind.Video, post.Media.RedditVideo.DashUrl);
            }

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