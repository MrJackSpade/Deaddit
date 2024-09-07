﻿using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Interfaces
{
    public interface IRedditClient
    {
        public string LoggedInUser { get; }

        Task<ApiSubReddit> About(SubRedditName subreddit);

        Task<ApiComment> Comment(ApiThing replyTo, string comment);

        IAsyncEnumerable<ApiThing> Comments(ApiPost thing, ApiComment? focus);

        Task Delete(ApiThing thing);

        IAsyncEnumerable<ApiThing> GetPosts(SubRedditName subreddit, ApiPostSort sort = ApiPostSort.Hot, string? after = null, Region region = Region.GLOBAL);

        Task<Stream> GetStream(string url);

        IAsyncEnumerable<ApiThing> MoreComments(ApiPost thing, ApiMore comment);

        Task SetUpvoteState(ApiThing thing, UpvoteState state);

        Task ToggleInboxReplies(ApiThing thing, bool enabled);

        Task ToggleSubScription(ApiSubReddit thing, bool subscribed);

        Task ToggleVisibility(ApiThing thing, bool visible);
    }
}