using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;

namespace Deaddit.Core.Reddit.Extensions
{
    public static class RedditPostExtensions
    {
        public static PostItems GetPostItems(this ApiPost post)
        {
            //v.reddit links won't currently resolve if not uploaded as part of 
            //the original post, as reddit isn't smart enough to copy over 
            //media metadata for its own links. 

            if (post.IsSelf)
            {
                return new PostItems(PostTargetKind.Post);
            }

            if (post.IsGallery == true)
            {
                PostItems items = new(PostTargetKind.Image);

                int i = 0;

                List<string> galleryItems = post.GalleryData.Items.Select(g => g.MediaId).ToList();

                Dictionary<string, MediaMetaData>? mediaMeta = post.MediaMetaData;

                List<MediaMetaData> sortedMedia = mediaMeta.Values.OrderBy(v => galleryItems.IndexOf(v.Id)).ToList();

                foreach (string imageUrl in sortedMedia.Select(m => m.Source.Url ?? m.Source.Gif))
                {
                    items.Add(new PostItem()
                    {
                        DownloadUrl = imageUrl,
                        LaunchUrl = imageUrl,
                        FileName = $"{post.Id}_{i++}{UrlHelper.GetExtension(imageUrl)}"
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
                        DownloadUrl = post.Media.RedditVideo.FallbackUrl,
                        LaunchUrl = post.Media.RedditVideo.DashUrl,
                        FileName = $"{post.Id}{UrlHelper.GetExtension(post.Media.RedditVideo.FallbackUrl)}"
                    }
                };

                return items;
            }

            Ensure.NotNullOrWhiteSpace(post.Url);

            return UrlHelper.Resolve(post.Url);
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