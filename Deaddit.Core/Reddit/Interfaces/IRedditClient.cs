using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.Requests;
using Deaddit.Core.Reddit.Models.ThingDefinitions;

namespace Deaddit.Core.Reddit.Interfaces
{
    public interface IRedditClient
    {
        /// <summary>
        /// True if the client can log in, in the current context.
        /// </summary>
        bool CanLogIn { get; }

        /// <summary>
        /// True if the client has valid credentials regardless of whether
        /// or not it is currently authenticated
        /// </summary>
        bool HasCredentials { get; }

        /// <summary>
        /// True if the client is currently authenticated
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// The username of the currently authenticated user
        /// </summary>
        public string? LoggedInUser { get; }

        Task<ApiSubReddit> About(SubRedditDefinition subreddit);

        Task<ApiComment> Comment(ApiThing replyTo, string comment);

        Task<List<ApiThing>> Comments(ApiPost thing, ApiComment? focus);

        Task Delete(ApiThing thing);

        Task<Dictionary<string, UserPartial>> GetPartialUserData(IEnumerable<string> usernames);

        Task<ApiPost> GetPost(string id);

        Task<List<ApiThing>> GetPosts<T>(ApiEndpointDefinition endpointDefinition, T sort, int pageSize, string? after = null, Region region = Region.GLOBAL) where T : Enum;

        Task<Stream> GetStream(string url);

        Task<ApiUser> GetUserData(string username);

        Task MarkRead(ApiThing message, bool state);

        Task<List<ApiThing>> MoreComments(ApiPost thing, IMore comment);

        Task<List<ApiMulti>> Multis();

        Task SetUpvoteState(ApiThing thing, UpvoteState state);

        Task ToggleInboxReplies(ApiThing thing, bool enabled);

        Task ToggleSave(ApiThing thing, bool saved);

        Task ToggleSubScription(ApiSubReddit thing, bool subscribed);

        Task ToggleVisibility(ApiThing thing, bool visible);

        Task<ApiComment> Update(ApiThing thing);
    }
}