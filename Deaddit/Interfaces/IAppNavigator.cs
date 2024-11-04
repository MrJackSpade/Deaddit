using Deaddit.Components;
using Deaddit.Components.WebComponents;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Models;
using Reddit.Api.Interfaces;
using Reddit.Api.Models;
using Reddit.Api.Models.Api;
using Reddit.Api.Models.ThingDefinitions;
using Deaddit.Core.Utils;
using Deaddit.Pages;

namespace Deaddit.Interfaces
{
    public interface IAppNavigator
    {
        RedditCommentWebComponent CreateCommentWebComponent(ApiComment newComment, ApiPost post, SelectionGroup selectionGroup);

        RedditMessageWebComponent CreateMessageWebComponent(ApiMessage message, SelectionGroup selectionGroup);

        MoreCommentsWebComponent CreateMoreCommentsWebComponent(IMore more);

        RedditPostWebComponent CreatePostWebComponent(ApiPost post, PostState postHandling, SelectionGroup? selectionGroup = null);

        SubscriptionComponent CreateSubRedditComponent(ThingDefinition subscriptionThing, SelectionGroup? group = null);

        Task<EmbeddedBrowser> OpenBrowser(string url);

        Task<ReplyPage> OpenEditPage(ApiThing toEdit);

        Task<EmbeddedImage> OpenImages(FileDownload[] urls);

        Task<MessagePage> OpenMessagePage(ApiUser user, ApiMessage? replyTo = null);

        Task<ThingCollectionPage> OpenMessages(InboxSort sort = InboxSort.Unread);

        Task<ObjectEditorPage> OpenObjectEditor(object original);

        Task OpenObjectEditor(Action onSave);

        Task OpenObjectEditor();

        Task<PostPage> OpenPost(ApiPost post, ApiComment focus = null);

        Task<ReplyPage> OpenReplyPage(ApiThing replyTo);

        Task<ThingCollectionPage> OpenSubReddit(string subRedditName, ApiPostSort sort = ApiPostSort.Hot);

        Task<SubRedditAboutPage> OpenSubRedditAbout(string subredditName);

        Task<ThingCollectionPage> OpenThing(ThingDefinition subRedditName);

        Task<ThingCollectionPage> OpenUser(string username, UserProfileSort userProfileSort = UserProfileSort.New);

        Task<EmbeddedVideo> OpenVideo(FileDownload url);
    }
}