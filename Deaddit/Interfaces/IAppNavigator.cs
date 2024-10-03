﻿using Deaddit.Components.WebComponents;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.MAUI.Components;
using Deaddit.Pages;

namespace Deaddit.Interfaces
{
    public interface IAppNavigator
    {
        RedditCommentWebComponent CreateCommentWebComponent(ApiComment newComment, ApiPost post, SelectionGroup selectionGroup);

        RedditMessageWebComponent CreateMessageWebComponent(ApiComment message);

        MoreCommentsWebComponent CreateMoreCommentsWebComponent(IMore more);

        RedditPostWebComponent CreatePostWebComponent(ApiPost post, bool blocked, SelectionGroup? selectionGroup = null);

        SubRedditComponent CreateSubRedditComponent(SubRedditSubscription subscription, SelectionGroup? group = null);

        Task<EmbeddedBrowser> OpenBrowser(string url);

        Task<ReplyPage> OpenEditPage(ApiThing toEdit);

        Task<EmbeddedImage> OpenImages(FileDownload[] urls);

        Task<SubRedditPage> OpenMessages(InboxSort sort = InboxSort.Unread);

        Task<ObjectEditorPage> OpenObjectEditor(object original);

        Task OpenObjectEditor(Action onSave);

        Task OpenObjectEditor();

        Task<PostPage> OpenPost(ApiPost post, ApiComment focus = null);

        Task<ReplyPage> OpenReplyPage(ApiThing replyTo);

        Task<SubRedditPage> OpenSubReddit(ThingCollectionName subRedditName, ApiPostSort sort = ApiPostSort.Hot);

        Task<SubRedditPage> OpenSubReddit(string subRedditName, ApiPostSort sort = ApiPostSort.Hot);

        Task<SubRedditAboutPage> OpenSubRedditAbout(ThingCollectionName subreddit);

        Task<SubRedditPage> OpenUser(string username, UserProfileSort userProfileSort = UserProfileSort.New);

        Task<EmbeddedVideo> OpenVideo(FileDownload url);
    }
}