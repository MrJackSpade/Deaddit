using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Handlers.Post
{
    internal class AggregatePostHandler(IEnumerable<IApiPostHandler> handlers, IAggregateUrlHandler urlHandler) : IAggregatePostHandler
    {
        private readonly List<IApiPostHandler> _handlers = handlers.ToList();

        public IAggregateUrlHandler UrlHandler { get; } = urlHandler;

        public bool CanDownload(ApiPost apiPost)
        {
            return _handlers.Any(h => h.CanDownload(apiPost, this));
        }

        public bool CanLaunch(ApiPost apiPost)
        {
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
                if (handler.CanLaunch(apiPost, this))
                {
                    await handler.Download(apiPost, this);
                }
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
                if (handler.CanLaunch(apiPost, this))
                {
                    await handler.Share(apiPost, this);
                }
            }

            throw new NotSupportedException();
        }
    }
}