using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Extensions
{
    public static class RedditPostExtensions
    {
        public static string? TryGetThumbnail(this ApiPost redditPost)
        {
            if (redditPost?.Thumbnail?.Contains("://") ?? false)
            {
                return redditPost?.Thumbnail;
            }

            if (redditPost?.Preview?.Images is null ||
                redditPost.Preview.Images.Count == 0)
            {
                return null;
            }

            return redditPost.Preview.Images[0].Source?.Url;
        }
    }
}