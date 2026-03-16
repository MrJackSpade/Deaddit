using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.IO;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Utils;
using Reddit.Api.Models.Json.Listings;
using DefaultShare = Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Deaddit.Handlers.Post
{
    internal class RedditGalleryHandler(IAppNavigator appNavigator, ApplicationHacks applicationHacks) : IApiPostHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        private readonly ApplicationHacks _applicationHacks = applicationHacks;

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
            IStreamConverter? converter = _applicationHacks.ConvertGifsToMp4 ? new GifToMp4Converter() : null;
            await FileStorage.Save(this.GetSortedGalleryItems(apiPost), converter);
        }

        public async Task Launch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            await _appNavigator.OpenImages([.. this.GetSortedGalleryItems(apiPost)]);
        }

        public async Task Share(ApiPost apiPost, IAggregatePostHandler caller)
        {
            IStreamConverter? converter = _applicationHacks.ConvertGifsToMp4 ? new GifToMp4Converter() : null;
            await DefaultShare.Share.Default.ShareFiles(apiPost.Title, this.GetSortedGalleryItems(apiPost), converter);
        }

        private List<FileDownload> GetSortedGalleryItems(ApiPost apiPost)
        {
            List<FileDownload> toReturn = [];

            List<string> galleryItems = apiPost.GalleryData.Items.Select(g => g.MediaId).ToList();

            Dictionary<string, MediaMetadata>? mediaMeta = apiPost.MediaMetaData;

            List<MediaMetadata> sortedMedia = [.. mediaMeta.Values.OrderBy(v => galleryItems.IndexOf(v.Id))];

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