using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using System.Diagnostics.CodeAnalysis;

namespace Deaddit.Core.Reddit.Extensions
{
    public static class RedditPostExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="post"></param>
        /// <param name="resolveUrl">if false, returns null instead of browser</param>
        /// <returns></returns>
        public static PostItems? GetPostItems(this ApiPost post, [NotNullWhen(true)] bool resolveUrl = true)
        {
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

            //Recurse up parent tree to see if we can find more information
            //before falling back on the browser
            foreach(ApiPost crossPost in post.CrossPostParentList)
            {
                PostItems? checkCrossPost = crossPost.GetPostItems(false);

                if(checkCrossPost is not null)
                {
                    return checkCrossPost;
                }
            }

            Ensure.NotNullOrWhiteSpace(post.Url);

            if(resolveUrl)
            {
                return UrlHelper.Resolve(post.Url);
            } else
            {
                return null;
            }
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