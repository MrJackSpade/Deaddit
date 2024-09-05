using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Region = Deaddit.Reddit.Models.Region;

namespace Deaddit.Reddit.Interfaces
{
    public interface IRedditClient
    {
        public string LoggedInUser { get; }

        Task<ApiSubReddit> About(SubRedditName subreddit);

        Task<ApiCommentMeta> Comment(ApiThing replyTo, string comment);

        IAsyncEnumerable<ApiCommentMeta> Comments(ApiPost thing, ApiComment? focus);
        Task Delete(ApiThing thing);

        IAsyncEnumerable<ApiPost> GetPosts(SubRedditName subreddit, ApiPostSort sort = ApiPostSort.Hot, string? after = null, Region region = Region.GLOBAL);

        Task<Stream> GetStream(string url);

        IAsyncEnumerable<ApiCommentMeta> MoreComments(ApiPost thing, ApiComment comment);

        Task SetUpvoteState(ApiThing thing, UpvoteState state);

        Task ToggleInboxReplies(ApiThing thing, bool enabled);

        Task ToggleSubScription(ApiSubReddit thing, bool subscribed);

        Task ToggleVisibility(ApiThing thing, bool visible);
    }
}