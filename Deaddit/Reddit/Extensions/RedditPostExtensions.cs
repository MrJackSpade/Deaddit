using Deaddit.Reddit.Exceptions;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;

namespace Deaddit.Reddit.Extensions
{
    public static class RedditPostExtensions
    {
        public static PostTarget GetResource(this ApiPost post)
        {
            if (post.IsSelf)
            {
                return new PostTarget(PostTargetKind.Post, null);
            }

            if (post.IsGallery == true)
            {
                List<string> imageUrls = post.MediaMetaData.Values.Select(m => m.Source.Url ?? m.Source.Gif).ToList();

                return new PostTarget(PostTargetKind.Image, imageUrls.ToArray());
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