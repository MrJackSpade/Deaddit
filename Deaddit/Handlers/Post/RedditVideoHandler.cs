using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils.Validation;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Utils;
using DefaultShare = Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Deaddit.Handlers.Post
{
    internal class RedditVideoHandler(IAppNavigator appNavigator, SavePathConfiguration savePaths) : IApiPostHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        private readonly SavePathConfiguration _savePaths = savePaths;

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
            Ensure.NotNull(apiPost.Media?.RedditVideo?.DashUrl);

            await FileStorage.Save(this.GetDownload(apiPost), savePaths: _savePaths);
        }

        public async Task Launch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(apiPost.Media);

            await _appNavigator.OpenVideo(this.GetDownload(apiPost));
        }

        public async Task Share(ApiPost apiPost, IAggregatePostHandler caller)
        {
            Ensure.NotNull(apiPost.Title);
            Ensure.NotNull(apiPost.Media?.RedditVideo?.DashUrl);

            await DefaultShare.Share.Default.ShareFiles(apiPost.Title, this.GetDownload(apiPost));
        }

        private FileDownload GetDownload(ApiPost apiPost)
        {
            Ensure.NotNull(apiPost.Media?.RedditVideo?.DashUrl);

            string dashUrl = apiPost.Media.RedditVideo.DashUrl;
            string name = $"{apiPost.Id}.mp4";

            return new FileDownload(name, dashUrl, dashUrl);
        }
    }
}