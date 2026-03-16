using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Interfaces;

namespace Deaddit.Handlers.Url
{
    internal class GiphyHandler(IAppNavigator appNavigator) : IUrlHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanDownload(string url, IAggregatePostHandler? aggregatePostHandler)
        {
            return this.IsGiphy(url);
        }

        public bool CanInline(string url)
        {
            return this.IsGiphy(url);
        }

        public bool CanLaunch(string url, IAggregatePostHandler aggregatePostHandler)
        {
            return this.IsGiphy(url);
        }

        public Task<FileDownload> Download(string url, IAggregatePostHandler aggregatePostHandler)
        {
            string imageUrl = this.GetDirectUrl(url);
            return Task.FromResult(new FileDownload("giphy.gif", imageUrl));
        }

        public string? GetInlineUrl(string url)
        {
            return this.GetDirectUrl(url);
        }

        public async Task Launch(string url, IAggregatePostHandler caller)
        {
            string imageUrl = this.GetDirectUrl(url);
            await _appNavigator.OpenImages([new FileDownload(imageUrl)]);
        }

        private string GetDirectUrl(string url)
        {
            Uri uri = new(url);

            // Already a direct media URL
            if (uri.Host.Contains("media") || uri.Host == "i.giphy.com")
            {
                return url;
            }

            // https://giphy.com/gifs/some-slug-ID -> extract ID (last segment after last hyphen)
            string lastSegment = uri.AbsolutePath.Split('/').Last();
            string id = lastSegment.Contains('-') ? lastSegment[(lastSegment.LastIndexOf('-') + 1)..] : lastSegment;

            return $"https://i.giphy.com/{id}.gif";
        }

        private bool IsGiphy(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
                   uri.Host.EndsWith("giphy.com", StringComparison.OrdinalIgnoreCase);
        }
    }
}
