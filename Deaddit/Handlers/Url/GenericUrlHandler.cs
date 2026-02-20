using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Interfaces;

namespace Deaddit.Handlers.Url
{
    internal class GenericUrlHandler(IAppNavigator appNavigator) : IUrlHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanLaunch(string url, IAggregatePostHandler aggregatePostHandler)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        public bool CanDownload(string url, IAggregatePostHandler? aggregatePostHandler)
        {
            return false;
        }

        public async Task Launch(string url, IAggregatePostHandler caller)
        {
            await _appNavigator.OpenBrowser(url);
        }

        public Task<FileDownload> Download(string url, IAggregatePostHandler aggregatePostHandler)
        {
            throw new NotSupportedException();
        }
    }
}