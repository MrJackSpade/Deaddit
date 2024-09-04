using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Region = Deaddit.Reddit.Models.Region;

namespace Deaddit.Reddit.Interfaces
{
    public interface IRedditClient
    {
        public string LoggedInUser { get; }
        Task<RedditCommentMeta> Comment(RedditThing replyTo, string comment);

        IAsyncEnumerable<RedditCommentMeta> Comments(RedditPost thing, string commentId);

        IAsyncEnumerable<RedditCommentMeta> MoreComments(RedditPost thing, RedditComment comment);

        Task<Stream> GetStream(string url);

        IAsyncEnumerable<RedditPost> Read(string subreddit, string sort = "hot", string? after = null, Region region = Region.GLOBAL);

        Task SetUpvoteState(RedditThing thing, UpvoteState state);

        Task ToggleInboxReplies(RedditThing thing, bool enabled);
    }
}