using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Utils;
using Reddit.Api.Models.Api;

namespace Deaddit.Handlers.Post
{
    internal class AggregatePostHandler(IEnumerable<IApiPostHandler> handlers, IAggregateUrlHandler urlHandler) : IAggregatePostHandler
    {
        private readonly List<IApiPostHandler> _handlers = handlers.ToList();

        public IAggregateUrlHandler UrlHandler { get; } = urlHandler;

        public bool CanDownload(ApiPost apiPost)
        {
            return _handlers.Any(h => h.CanDownload(apiPost, this)) || UrlHandler.CanDownload(apiPost.Url, this);
        }

        public bool CanLaunch(ApiPost apiPost)
        {
            if (string.IsNullOrWhiteSpace(apiPost.Url))
            {
                return false;
            }

            return UrlHandler.CanLaunch(apiPost.Url, this) || _handlers.Any(h => h.CanLaunch(apiPost, this));
        }

        public bool CanShare(ApiPost apiPost)
        {
            return _handlers.Any(h => h.CanShare(apiPost, this));
        }

        public async Task Download(ApiPost apiPost)
        {
            foreach (IApiPostHandler handler in _handlers)
            {
                if (handler.CanDownload(apiPost, this))
                {
                    await handler.Download(apiPost, this);
                    return;
                }
            }

            if (UrlHandler.CanDownload(apiPost.Url, this))
            {
                FileDownload download = await UrlHandler.Download(apiPost.Url, this);
                await FileStorage.Save([download]);
                return;
            }

            throw new NotSupportedException();
        }

        public async Task Launch(ApiPost apiPost)
        {
            foreach (IApiPostHandler handler in _handlers)
            {
                if (handler.CanLaunch(apiPost, this))
                {
                    await handler.Launch(apiPost, this);
                    return;
                }
            }

            if (UrlHandler.CanLaunch(apiPost.Url, this))
            {
                await UrlHandler.Launch(apiPost.Url, this);
                return;
            }

            throw new NotSupportedException();
        }

        public async Task Share(ApiPost apiPost)
        {
            foreach (IApiPostHandler handler in _handlers)
            {
                if (handler.CanShare(apiPost, this))
                {
                    await handler.Share(apiPost, this);
                    return;
                }
            }

            throw new NotSupportedException();
        }
    }
}