﻿using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Utils;
using DefaultShare = Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Deaddit.Handlers.Post
{
    internal class RedditVideoHandler(IAppNavigator appNavigator) : IApiPostHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanDownload(ApiPost apiPost, IAggregatePostHandler caller)
        {
            return apiPost.Media?.RedditVideo is not null;
        }

        public bool CanLaunch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            return apiPost.Media?.RedditVideo is not null;
        }

        public bool CanShare(ApiPost apiPost, IAggregatePostHandler caller)
        {
            return apiPost.Media?.RedditVideo is not null;
        }

        public async Task Download(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(apiPost.Media?.RedditVideo?.FallbackUrl);

            await FileStorage.Save(this.GetDownload(apiPost));
        }

        public async Task Launch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(apiPost.Media);

            await _appNavigator.OpenVideo(this.GetDownload(apiPost));
        }

        public async Task Share(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(apiPost.Title);
            Ensure.NotNull(apiPost.Media?.RedditVideo?.FallbackUrl);

            await DefaultShare.Share.Default.ShareFiles(apiPost.Title, this.GetDownload(apiPost));
        }

        private FileDownload GetDownload(ApiPost apiPost)
        {
            Ensure.NotNull(apiPost.Media?.RedditVideo?.FallbackUrl);

            string url = apiPost.Media.RedditVideo.FallbackUrl;

            string name = $"{apiPost.Id}{UrlHelper.GetExtension(url)}";

            return new FileDownload(name, url);
        }
    }
}