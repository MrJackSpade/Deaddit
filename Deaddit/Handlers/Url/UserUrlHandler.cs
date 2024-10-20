using Deaddit.Core.Interfaces;
using Deaddit.Core.Utils.Validation;
using Deaddit.Interfaces;
using System.Text.RegularExpressions;

namespace Deaddit.Handlers.Url
{
    internal class UserUrlHandler(IAppNavigator appNavigator) : IUrlHandler
    {
        private const string SubredditRegex = @"\/u\/([a-zA-Z0-9_]+)/?$";

        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanLaunch(string url, IAggregatePostHandler aggregatePostHandler)
        {
            if (aggregatePostHandler is null)
            {
                return false;
            }

            return Regex.IsMatch(url, SubredditRegex);
        }

        public async Task Launch(string url, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            Match match = Regex.Match(url, SubredditRegex, RegexOptions.IgnoreCase);

            string id = match.Groups[1].Value;

            await _appNavigator.OpenUser(id);
        }
    }
}