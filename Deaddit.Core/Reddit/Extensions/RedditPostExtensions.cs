using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;

namespace Deaddit.Core.Reddit.Extensions
{
    public static class RedditPostExtensions
    {
        public static PostItems GetPostItems(this ApiPost post)
        {
            if (post.IsSelf)
            {
                return new PostItems(PostTargetKind.Post);
            }

            if (post.IsGallery == true)
            {
                PostItems items = new(PostTargetKind.Image);

                int i = 0;

                foreach (string imageUrl in post.MediaMetaData.Values.Select(m => m.Source.Url ?? m.Source.Gif))
                {
                    items.Add(new PostItem()
                    {
                        DownloadUrl = imageUrl,
                        LaunchUrl = imageUrl,
                        FileName = $"{post.Id}_{i++}{UrlHandler.GetExtension(imageUrl)}"
                    });
                }

                return items;
            }

            if (post.Media?.RedditVideo is not null)
            {
                PostItems items = new(PostTargetKind.Video)
                {
                    new PostItem()
                    {
                        DownloadUrl = post.Media.RedditVideo.DashUrl,
                        LaunchUrl = post.Media.RedditVideo.FallbackUrl,
                        FileName = $"{post.Id}{UrlHandler.GetExtension(post.Media.RedditVideo.FallbackUrl)}"
                    }
                };

                return items;
            }

            Ensure.NotNullOrWhiteSpace(post.Url);

            return Resolve(post.Url);
        }

        public static PostItems Resolve(string url)
        {
            string mimeType = UrlHandler.GetMimeTypeFromUri(new Uri(url));

            PostItems items;
            // Switch based on the type
            if (mimeType.StartsWith("image/"))
            {
                items = new(PostTargetKind.Image);
            }
            else if (mimeType.StartsWith("audio/"))
            {
                items = new(PostTargetKind.Audio);
            }
            else if (mimeType.StartsWith("video/"))
            {
                items = new(PostTargetKind.Video);
            }
            else
            {
                items = new(PostTargetKind.Url);
            }

            items.Add(new PostItem()
            {
                DownloadUrl = url,
                LaunchUrl = url,
                FileName = UrlHandler.GetFileName(url)
            });

            return items;
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