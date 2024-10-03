using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Utils;
using Deaddit.Interfaces;

namespace Deaddit.Handlers.Url
{
    internal class GenericImageHandler(IAppNavigator appNavigator) : IUrlHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanLaunch(string url, IAggregatePostHandler aggregatePostHandler)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return false;
            }

            string mimeType = UrlHelper.GetMimeTypeFromUri(new Uri(url));

            return mimeType.StartsWith("image/");
        }

        public async Task Launch(string url, IAggregatePostHandler caller)
        {
            await _appNavigator.OpenImages([new FileDownload(url)]);
        }
    }
}