using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Utils;
using Deaddit.Interfaces;

namespace Deaddit.Handlers.Url
{
    internal class GenericVideoHandler(IAppNavigator appNavigator) : IUrlHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanLaunch(string url, IAggregatePostHandler aggregatePostHandler)
        {
            return this.IsVideo(url);
        }

        public bool CanDownload(string url, IAggregatePostHandler? aggregatePostHandler)
        {
            return this.IsVideo(url);
        }

        public async Task Launch(string url, IAggregatePostHandler caller)
        {
            await _appNavigator.OpenVideo(new FileDownload(url));
        }

        public Task<FileDownload> Download(string url, IAggregatePostHandler aggregatePostHandler)
        {
            string fileName = UrlHelper.GetFileName(url);
            return Task.FromResult(new FileDownload(fileName, url));
        }

        private bool IsVideo(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return false;
            }

            string mimeType = UrlHelper.GetMimeTypeFromUri(new Uri(url));

            return mimeType.StartsWith("video/");
        }
    }
}