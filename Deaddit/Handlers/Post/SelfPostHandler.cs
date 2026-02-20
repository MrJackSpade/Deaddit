using Deaddit.Core.Interfaces;
using Deaddit.Interfaces;
using Reddit.Api.Models.Api;
using DefaultShare = Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Deaddit.Handlers.Post
{
    internal class SelfPostHandler(IAppNavigator appNavigator) : IApiPostHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanDownload(ApiPost apiPost, IAggregatePostHandler caller)
        {
            return false;
        }

        public bool CanLaunch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            return apiPost.IsSelf;
        }

        public bool CanShare(ApiPost apiPost, IAggregatePostHandler caller)
        {
            return apiPost.IsSelf;
        }

        public Task Download(ApiPost apiPost, IAggregatePostHandler caller)
        {
            throw new NotImplementedException();
        }

        public async Task Launch(ApiPost apiPost, IAggregatePostHandler caller)
        {
            await _appNavigator.OpenPost(apiPost);
        }

        public async Task Share(ApiPost apiPost, IAggregatePostHandler caller)
        {
            await DefaultShare.Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = apiPost.Title,
                Text = apiPost.Url
            });
        }
    }
}