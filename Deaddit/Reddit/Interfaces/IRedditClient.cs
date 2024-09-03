using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Region = Deaddit.Reddit.Models.Region;

namespace Deaddit.Reddit.Interfaces
{
    public interface IRedditClient
    {
        Task<RedditCommentMeta> Comment(RedditThing replyTo, string comment);

        Task<List<CommentReadResponse>> Comments(RedditThing thing);

        Task<Stream> GetStream(string url);

        IAsyncEnumerable<RedditPost> Read(string subreddit, string sort = "hot", string? after = null, Region region = Region.GLOBAL);

        Task SetUpvoteState(RedditThing thing, UpvoteState state);
    }
}