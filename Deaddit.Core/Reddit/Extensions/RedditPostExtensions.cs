using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;

namespace Deaddit.Core.Reddit.Extensions
{
    public static class RedditPostExtensions
    {
        public static PostTarget GetPostTarget(this ApiPost post)
        {
            if (post.IsSelf)
            {
                return new PostTarget(PostTargetKind.Post);
            }

            if (post.IsGallery == true)
            {
                List<string> imageUrls = post.MediaMetaData.Values.Select(m => m.Source.Url ?? m.Source.Gif).ToList();

                return new PostTarget(PostTargetKind.Image, imageUrls.ToArray());
            }

            if (post.Media?.RedditVideo is not null)
            {
                return new PostTarget(PostTargetKind.Video, post.Media.RedditVideo.DashUrl, post.Media.RedditVideo.FallbackUrl);
            }

            Ensure.NotNullOrWhiteSpace(post.Url);

            return UrlHandler.Resolve(post.Url);
        }

        public static string? TryGetPreview(this ApiPost redditPost)
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