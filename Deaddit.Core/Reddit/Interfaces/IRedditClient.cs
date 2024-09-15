using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Interfaces
{
    public interface IRedditClient
    {
        bool CanLogIn { get; }

        public string? LoggedInUser { get; }

        Task<ApiSubReddit> About(ThingCollectionName subreddit);

        Task<ApiComment> Comment(ApiThing replyTo, string comment);

        IAsyncEnumerable<ApiThing> Comments(ApiPost thing, ApiComment? focus);

        Task Delete(ApiThing thing);

        IAsyncEnumerable<ApiThing> GetPosts<T>(ThingCollectionName subreddit, T sort, string? after = null, Region region = Region.GLOBAL) where T : Enum;

        Task<Stream> GetStream(string url);

        Task<Dictionary<string, UserPartial>> GetUserData(IEnumerable<string> usernames);

        IAsyncEnumerable<ApiThing> MoreComments(ApiPost thing, IMore comment);

        IAsyncEnumerable<ApiMulti> Multis();

        Task SetUpvoteState(ApiThing thing, UpvoteState state);

        Task ToggleInboxReplies(ApiThing thing, bool enabled);

        Task ToggleSave(ApiThing thing, bool saved);

        Task ToggleSubScription(ApiSubReddit thing, bool subscribed);

        Task ToggleVisibility(ApiThing thing, bool visible);

        Task<ApiComment> Update(ApiThing thing);
    }
}