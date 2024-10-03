using Deaddit.Core.Interfaces;

namespace Deaddit.Handlers.Url
{
    internal class AggregateUrlHandler(IEnumerable<IUrlHandler> handlers) : IAggregateUrlHandler
    {
        private readonly List<IUrlHandler> _handlers = handlers.ToList();

        public bool CanLaunch(string url, IAggregatePostHandler? aggregatePostHandler)
        {
            return _handlers.Any(h => h.CanLaunch(url, aggregatePostHandler));
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
    }
}