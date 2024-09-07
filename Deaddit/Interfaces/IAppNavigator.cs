﻿using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.MAUI.Components;
using Deaddit.Pages;

namespace Deaddit.Interfaces
{
    public interface IAppNavigator
    {
        RedditCommentComponent CreateCommentComponent(ApiComment comment, ApiPost? post = null, SelectionGroup? selectionGroup = null);

        MoreCommentsComponent CreateMoreCommentsComponent(ApiMore more);

        RedditPostComponent CreatePostComponent(ApiPost post, SelectionGroup? selectionGroup = null);

        SubRedditComponent CreateSubRedditComponent(SubRedditSubscription subscription, SelectionGroup? group = null);

        Task<EmbeddedBrowser> OpenBrowser(PostItems resource);

        Task<EmbeddedImage> OpenImage(PostItems resource);

        Task<ObjectEditorPage> OpenObjectEditor(object original);

        Task<PostPage> OpenPost(ApiPost post, ApiComment focus = null);

        Task<ReplyPage> OpenReplyPage(ApiThing replyTo);

        Task<SubRedditPage> OpenSubReddit(SubRedditName subRedditName, ApiPostSort sort = ApiPostSort.Hot);

        Task<SubRedditPage> OpenSubReddit(string subRedditName, ApiPostSort sort = ApiPostSort.Hot);

        Task<SubRedditAboutPage> OpenSubRedditAbout(SubRedditName subreddit);

        Task<SubRedditPage> OpenUser(string username, UserProfileSort userProfileSort = UserProfileSort.New);

        Task<EmbeddedVideo> OpenVideo(PostItems resource);
    }
}