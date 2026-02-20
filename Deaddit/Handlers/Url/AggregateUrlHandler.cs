using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;

namespace Deaddit.Handlers.Url
{
    internal class AggregateUrlHandler(IEnumerable<IUrlHandler> handlers) : IAggregateUrlHandler
    {
        private readonly List<IUrlHandler> _handlers = handlers.ToList();

        public bool CanLaunch(string url, IAggregatePostHandler? aggregatePostHandler)
        {
            return _handlers.Any(h => h.CanLaunch(url, aggregatePostHandler));
        }

        public bool CanDownload(string? url, IAggregatePostHandler? aggregatePostHandler)
        {
            return _handlers.Any(h => h.CanDownload(url, aggregatePostHandler));
        }

        public async Task Launch(string? url, IAggregatePostHandler aggregatePostHandler)
        {
            foreach (IUrlHandler handler in _handlers)
            {
                if (handler.CanLaunch(url, aggregatePostHandler))
                {
                    await handler.Launch(url, aggregatePostHandler);
                    return;
                }
            }

            throw new NotSupportedException();
        }

        public async Task<FileDownload> Download(string? url, IAggregatePostHandler aggregatePostHandler)
        {
            foreach (IUrlHandler handler in _handlers)
            {
                if (handler.CanDownload(url, aggregatePostHandler))
                {
                    return await handler.Download(url, aggregatePostHandler);
                }
            }

            throw new NotSupportedException();
        }
    }
}