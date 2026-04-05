using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;

using Deaddit.Core.Reddit.Models.Requests;
using Deaddit.Core.Reddit.Models.ThingDefinitions;
using Reddit.Api.Models.Enums;
using Reddit.Api.Models.Json.Multis;
using Reddit.Api.Models.Json.Subreddits;
using Reddit.Api.Models.Json.Users;

namespace Deaddit.Core.Reddit.Interfaces
{
    public interface IRedditClient
    {
        bool CanLogIn { get; }

        bool IsLoggedIn { get; }

        public string? LoggedInUser { get; }

        void SetTokenRefreshFunction(Func<Task<string?>> tokenRefreshFunc);

        Task<Subreddit?> About(SubRedditDefinition subreddit);

        Task<ApiComment> Comment(ApiThing replyTo, string markdown);

        Task<string> UploadMedia(Stream fileStream, string filename, string mimetype);

        Task<List<ApiThing>> Comments(ApiPost thing, CommentFocus? focus = null);

        Task Delete(ApiThing thing);

        Task<Dictionary<string, UserPartialData>> GetPartialUserData(IEnumerable<string> usernames);

        Task<ApiPost?> GetPost(string id);

        Task<List<ApiPost>> GetPosts(IEnumerable<string> ids);

        Task<List<ApiThing>> GetPosts<T>(ApiEndpointDefinition endpointDefinition, T sort, int pageSize, string? after = null, Region region = Region.GLOBAL) where T : Enum;

        Task<Stream> GetStream(string url);

        Task<User?> GetUserData(string username);

        Task MarkRead(ApiThing message, bool state);

        Task Message(User thing, string subject, string body);

        Task<List<ApiThing>> MoreComments(ApiPost thing, IMore comment);

        Task<bool> AddSubredditToMulti(Multi multi, string subreddit);

        Task<Multi?> CreateMulti(string name);

        Task<bool> DeleteMulti(Multi multi);

        Task<List<Multi>> Multis();

        Task<bool> RemoveSubredditFromMulti(Multi multi, string subreddit);

        Task SetVoteState(ApiThing thing, VoteState state);

        Task ToggleInboxReplies(ApiThing thing, bool enabled);

        Task ToggleSave(ApiThing thing, bool saved);

        Task ToggleSubScription(Subreddit thing, bool subscribed);

        Task ToggleVisibility(ApiThing thing, bool visible);

        Task<ApiComment> Update(ApiThing thing);

        Task Report(ApiThing thing, string? reason = null, string? siteReason = null, string? ruleReason = null);

        Task<SubredditRulesResponse?> GetSubredditRules(string subreddit);
    }
}