using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Region = Deaddit.Reddit.Models.Region;

namespace Deaddit.Reddit.Interfaces
{
    public interface IRedditClient
    {
        public string LoggedInUser { get; }

        Task<ApiSubReddit> About(string subreddit);

        Task<RedditCommentMeta> Comment(ApiThing replyTo, string comment);

        IAsyncEnumerable<RedditCommentMeta> Comments(ApiPost thing, string commentId);

        Task<Stream> GetStream(string url);

        IAsyncEnumerable<RedditCommentMeta> MoreComments(ApiPost thing, ApiComment comment);

        IAsyncEnumerable<ApiPost> GetPosts(string subreddit, string sort = "hot", string? after = null, Region region = Region.GLOBAL);

        Task SetUpvoteState(ApiThing thing, UpvoteState state);

        Task ToggleInboxReplies(ApiThing thing, bool enabled);

        Task ToggleSubScription(ApiSubReddit thing, bool subscribed);

        Task ToggleVisibility(ApiThing thing, bool visible);
    }
}