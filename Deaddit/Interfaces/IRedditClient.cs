using Deaddit.Models;
using Deaddit.Models.Json.Response;

namespace Deaddit.Interfaces
{
    public interface IRedditClient
    {
        Task<RedditCommentMeta> Comment(RedditThing replyTo, string comment);

        Task<List<CommentReadResponse>> Comments(RedditThing thing);

        Task<Stream> GetStream(string url);

        IAsyncEnumerable<RedditPost> Read(string subreddit, string sort = "hot", string? after = null, RedditRegion region = RedditRegion.GLOBAL);

        Task SetUpvoteState(RedditThing thing, UpvoteState state);
    }
}