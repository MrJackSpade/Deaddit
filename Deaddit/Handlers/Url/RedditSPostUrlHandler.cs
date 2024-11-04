using Deaddit.Core.Interfaces;
using Reddit.Api.Interfaces;
using Deaddit.Core.Utils.Validation;
using Deaddit.Interfaces;
using System.Text.RegularExpressions;

namespace Deaddit.Handlers.Url
{
    internal class RedditSPostUrlHandler(IRedditClient redditClient, IAppNavigator appNavigator) : IUrlHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        private readonly IRedditClient _redditClient = redditClient;

        public bool CanLaunch(string url, IAggregatePostHandler aggregatePostHandler)
        {
            if (aggregatePostHandler is null)
            {
                return false;
            }

            return Regex.IsMatch(url, @".*/r\/[^\/]+\/s\/[^\/]+/?.*$", RegexOptions.IgnoreCase);
        }

        public async Task Launch(string url, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            HttpClientHandler handler = new()
            {
                AllowAutoRedirect = false
            };

            HttpClient httpClient = new(handler);

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Deaddit");

            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);

            string newUrl = httpResponseMessage.Headers.Location.ToString();

            if (newUrl != url)
            {
                await caller.UrlHandler.Launch(newUrl, caller);
            }
        }
    }
}