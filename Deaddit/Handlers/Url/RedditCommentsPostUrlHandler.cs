using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils.Validation;
using Deaddit.Interfaces;
using System.Text.RegularExpressions;

namespace Deaddit.Handlers.Url
{
    internal class RedditCommentsPostUrlHandler(IRedditClient redditClient, IAppNavigator appNavigator) : IUrlHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        private readonly IRedditClient _redditClient = redditClient;

        public bool CanDownload(string url, IAggregatePostHandler? aggregatePostHandler)
        {
            return false;
        }

        public bool CanLaunch(string url, IAggregatePostHandler aggregatePostHandler)
        {
            if (aggregatePostHandler is null)
            {
                return false;
            }

            return Regex.IsMatch(url, @".*/r\/[^\/]+\/comments\/[^\/]+\/[^\/]+\/?.*$");
        }

        public Task<FileDownload> Download(string url, IAggregatePostHandler aggregatePostHandler)
        {
            throw new NotSupportedException();
        }

        public async Task Launch(string url, IAggregatePostHandler caller)
        {
            Ensure.NotNull(caller);

            // Match post ID and optional comment ID: /r/sub/comments/postid/slug/commentid/
            Match match = Regex.Match(url, @"\/r\/[^\/]+\/comments\/([a-z0-9]+)(?:\/[^\/]*\/([a-z0-9]+))?", RegexOptions.IgnoreCase);

            string postId = match.Groups[1].Value;
            string? commentId = match.Groups[2].Success ? match.Groups[2].Value : null;

            ApiPost post = await _redditClient.GetPost(postId);

            ApiComment? focusComment = commentId != null ? new ApiComment { Id = commentId } : null;

            await _appNavigator.OpenPost(post, focusComment);
        }
    }
}