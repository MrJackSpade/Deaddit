using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Utils.Validation;
using Deaddit.Interfaces;
using Reddit.Api.Interfaces;
using Reddit.Api.Models.Api;
using System.Text.RegularExpressions;

namespace Deaddit.Handlers.Url
{
    internal class RedditCommentsPostUrlHandler(IRedditClient redditClient, IAppNavigator appNavigator) : IUrlHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        private readonly IRedditClient _redditClient = redditClient;

        public bool CanLaunch(string url, IAggregatePostHandler aggregatePostHandler)
        {
            if (aggregatePostHandler is null)
            {
                return false;
            }

            return Regex.IsMatch(url, @".*/r\/[^\/]+\/comments\/[^\/]+\/[^\/]+\/?.*$");
        }

        public bool CanDownload(string url, IAggregatePostHandler? aggregatePostHandler)
        {
            return false;
        }

        public async Task Launch(string url, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            Match match = Regex.Match(url, @"\/r\/[^\/]+\/comments\/([a-z0-9]+)/", RegexOptions.IgnoreCase);

            string id = match.Groups[1].Value;

            ApiPost post = await _redditClient.GetPost(id);

            await _appNavigator.OpenPost(post);
        }

        public Task<FileDownload> Download(string url, IAggregatePostHandler aggregatePostHandler)
        {
            throw new NotSupportedException();
        }
    }
}