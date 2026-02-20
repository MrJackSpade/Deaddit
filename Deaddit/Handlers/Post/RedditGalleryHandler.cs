using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Utils;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Utils;
using Reddit.Api.Models.Api;
using DefaultShare = Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Deaddit.Handlers.Post
{
    internal class RedditGalleryHandler(IAppNavigator appNavigator) : IApiPostHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanDownload(ApiPost apiPost, IAggregatePostHandler caller)
        {
            return apiPost.GalleryData?.Items?.Any() ?? false;
        }

        public bool CanLaunch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            return apiPost.GalleryData?.Items?.Any() ?? false;
        }

        public bool CanShare(ApiPost apiPost, IAggregatePostHandler caller)
        {
            return apiPost.IsGallery == true;
        }

        public async Task Download(ApiPost apiPost, IAggregatePostHandler caller)
        {
            await FileStorage.Save(this.GetSortedGalleryItems(apiPost));
        }

        public async Task Launch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            await _appNavigator.OpenImages([.. this.GetSortedGalleryItems(apiPost)]);
        }

        public async Task Share(ApiPost apiPost, IAggregatePostHandler caller)
        {
            await DefaultShare.Share.Default.ShareFiles(apiPost.Title, this.GetSortedGalleryItems(apiPost));
        }

        private List<FileDownload> GetSortedGalleryItems(ApiPost apiPost)
        {
            List<FileDownload> toReturn = [];

            List<string> galleryItems = apiPost.GalleryData.Items.Select(g => g.MediaId).ToList();

            Dictionary<string, MediaMetaData>? mediaMeta = apiPost.MediaMetaData;

            List<MediaMetaData> sortedMedia = [.. mediaMeta.Values.OrderBy(v => galleryItems.IndexOf(v.Id))];

            foreach (string? imageUrl in sortedMedia.Select(m => m.Source?.Url ?? m.Source?.Gif))
            {
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    toReturn.Add(new FileDownload(
                        $"{apiPost.Id}_{toReturn.Count}{UrlHelper.GetExtension(imageUrl)}",
                        imageUrl
                    ));
                }
            }

            return toReturn;
        }
    }
}