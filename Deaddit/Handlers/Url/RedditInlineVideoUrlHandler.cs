using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Interfaces;
using System.Text.RegularExpressions;

namespace Deaddit.Handlers.Url
{
    /// <summary>
    /// A video embedded in a comment. Reddit writes it into the body as a placeholder link,
    /// reddit.com/link/{commentId}/video/{mediaId}/player, which only reddit's own site turns into
    /// a player; visiting it lands on a "subreddit is banned" page. The media id is the
    /// v.redd.it video, so play that directly.
    /// </summary>
    internal class RedditInlineVideoUrlHandler(IAppNavigator appNavigator) : IUrlHandler
    {
        private const string InlineVideoRegex = @"^https?://(?:[a-z]+\.)?reddit\.com/link/[a-z0-9]+/video/([a-z0-9]+)/player/?$";

        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanDownload(string url, IAggregatePostHandler? aggregatePostHandler) => IsInlineVideo(url);

        public bool CanInline(string url) => false;

        public bool CanLaunch(string url, IAggregatePostHandler aggregatePostHandler) => IsInlineVideo(url);

        public Task<FileDownload> Download(string url, IAggregatePostHandler aggregatePostHandler)
        {
            return Task.FromResult(GetDownload(url));
        }

        public string? GetInlineUrl(string url) => null;

        public async Task Launch(string url, IAggregatePostHandler caller)
        {
            await _appNavigator.OpenVideo(GetDownload(url));
        }

        private static FileDownload GetDownload(string url)
        {
            Match match = Regex.Match(url, InlineVideoRegex, RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                throw new ArgumentException($"Not a reddit inline video url: {url}", nameof(url));
            }

            string mediaId = match.Groups[1].Value;
            string dashUrl = $"https://v.redd.it/{mediaId}/DASHPlaylist.mpd";

            return new FileDownload($"{mediaId}.mp4", dashUrl, dashUrl);
        }

        private static bool IsInlineVideo(string? url)
        {
            return url is not null && Regex.IsMatch(url, InlineVideoRegex, RegexOptions.IgnoreCase);
        }
    }
}
