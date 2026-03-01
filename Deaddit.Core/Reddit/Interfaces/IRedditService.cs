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
    /// <summary>
    /// Interface for Reddit service operations.
    /// Provides business logic layer methods that work with Api models.
    /// </summary>
    public interface IRedditService
    {
        /// <summary>
        /// Whether valid credentials are available for login.
        /// </summary>
        bool CanLogIn { get; }

        /// <summary>
        /// Whether the service is currently logged in.
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// The logged in user's username, or null if not logged in.
        /// </summary>
        string? LoggedInUser { get; }

        /// <summary>
        /// Gets information about a subreddit.
        /// </summary>
        Task<Subreddit?> About(SubRedditDefinition subreddit);

        /// <summary>
        /// Posts a comment in reply to a thing.
        /// </summary>
        Task<ApiComment?> Comment(ApiThing replyTo, string comment);

        /// <summary>
        /// Gets comments for a post.
        /// </summary>
        Task<List<ApiThing>> Comments(ApiPost post, ApiComment? focusComment);

        /// <summary>
        /// Creates a new post.
        /// </summary>
        Task<ApiPost?> CreatePost(string subreddit, string title, string content, SubmitKind kind);

        /// <summary>
        /// Deletes a thing.
        /// </summary>
        Task Delete(ApiThing thing);

        /// <summary>
        /// Displays an exception to the user.
        /// Returns true if the exception was handled.
        /// </summary>
        Task<bool> DisplayException(Exception ex);

        /// <summary>
        /// Gets partial user data for multiple usernames.
        /// </summary>
        Task<Dictionary<string, UserPartialData>> GetPartialUserData(IEnumerable<string> usernames);

        /// <summary>
        /// Gets a single post by ID.
        /// </summary>
        Task<ApiPost?> GetPost(string id);

        /// <summary>
        /// Gets multiple posts by ID.
        /// </summary>
        Task<List<ApiPost>> GetPosts(IEnumerable<string> ids);

        /// <summary>
        /// Gets posts from an endpoint with sorting.
        /// </summary>
        Task<List<ApiThing>> GetPosts<T>(ApiEndpointDefinition endpointDefinition, T sort, int pageSize, string? after = null, Region region = Region.GLOBAL) where T : Enum;

        /// <summary>
        /// Gets a stream for a URL.
        /// </summary>
        Task<Stream> GetStream(string url);

        /// <summary>
        /// Gets user data by username.
        /// </summary>
        Task<User?> GetUserData(string username);

        /// <summary>
        /// Marks a message as read or unread.
        /// </summary>
        Task MarkRead(ApiThing message, bool state);

        /// <summary>
        /// Sends a message to a user.
        /// </summary>
        Task Message(User user, string subject, string body);

        /// <summary>
        /// Loads more comments.
        /// </summary>
        Task<List<ApiThing>> MoreComments(ApiPost post, IMore moreItem);

        /// <summary>
        /// Gets the user's multireddits.
        /// </summary>
        Task<List<Multi>> Multis();

        /// <summary>
        /// Sets the upvote state on a thing.
        /// </summary>
        Task SetVoteState(ApiThing thing, VoteState state);

        /// <summary>
        /// Toggles distinguish and sticky on a thing.
        /// </summary>
        Task ToggleDistinguish(ApiThing thing, bool distinguish, bool sticky);

        /// <summary>
        /// Toggles inbox replies for a thing.
        /// </summary>
        Task ToggleInboxReplies(ApiThing thing, bool enabled);

        /// <summary>
        /// Toggles lock state on a thing.
        /// </summary>
        Task ToggleLock(ApiThing thing, bool locked);

        /// <summary>
        /// Toggles save state on a thing.
        /// </summary>
        Task ToggleSave(ApiThing thing, bool saved);

        /// <summary>
        /// Toggles subscription to a subreddit.
        /// </summary>
        Task ToggleSubScription(Subreddit subreddit, bool subscribed);

        /// <summary>
        /// Toggles visibility (hide/unhide) on a thing.
        /// </summary>
        Task ToggleVisibility(ApiThing thing, bool visible);

        /// <summary>
        /// Updates/edits a thing's content.
        /// </summary>
        Task<ApiComment?> Update(ApiThing thing);
    }
}