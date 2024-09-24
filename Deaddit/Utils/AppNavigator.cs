using Deaddit.Components.WebComponents;
using Deaddit.Configurations;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components;
using Deaddit.Pages;

namespace Deaddit.Utils
{
    /// <summary>
    /// This is a weird hack because the number of references that need to be passed around
    /// is way too large, and the number of redundant component inits is high enough
    /// that wonky fucking magic like this is the better option. Since components cant
    /// be reasonably unit tested to begin with, code cleanliness is actually better
    /// </summary>
    public class AppNavigator(IRedditClient redditClient,
                              RedditCredentials redditCredentials,
                              ApplicationStyling applicationTheme,
                              ApplicationHacks applicationHacks,
                              IVisitTracker visitTracker,
                              BlockConfiguration blockConfiguration,
                              IConfigurationService configurationService) : IAppNavigator
    {
        private readonly ApplicationHacks _applicationHacks = Ensure.NotNull(applicationHacks);

        private readonly ApplicationStyling _applicationStyling = Ensure.NotNull(applicationTheme);

        private readonly BlockConfiguration _blockConfiguration = Ensure.NotNull(blockConfiguration);

        private readonly IConfigurationService _configurationService = Ensure.NotNull(configurationService);

        private readonly IRedditClient _redditClient = Ensure.NotNull(redditClient);

        private readonly RedditCredentials _redditCredentials = Ensure.NotNull(redditCredentials);

        private readonly IVisitTracker _visitTracker = Ensure.NotNull(visitTracker);

        private static INavigation Navigation => Shell.Current.Navigation;

        public RedditCommentComponent CreateCommentComponent(ApiComment comment, ApiPost? post = null, SelectionGroup? selectionGroup = null)
        {
            if (selectionGroup is null)
            {
                return new RedditCommentComponent(comment, post, false, this, _redditClient, _applicationStyling, selectionGroup ?? new SelectionGroup(), _blockConfiguration);
            }
            else
            {
                return new RedditCommentComponent(comment, post, true, this, _redditClient, _applicationStyling, selectionGroup ?? new SelectionGroup(), _blockConfiguration);
            }
        }

        public RedditMessageComponent CreateMessageComponent(ApiMessage message, SelectionGroup? selectionGroup = null)
        {
            if (selectionGroup is null)
            {
                return new RedditMessageComponent(message, false, this, _redditClient, _applicationStyling, selectionGroup ?? new SelectionGroup(), _blockConfiguration);
            }
            else
            {
                return new RedditMessageComponent(message, true, this, _redditClient, _applicationStyling, selectionGroup ?? new SelectionGroup(), _blockConfiguration);
            }
        }

        public MoreCommentsComponent CreateMoreCommentsComponent(IMore more)
        {
            return new MoreCommentsComponent(more, _applicationStyling);
        }

        public RedditPostWebComponent CreatePostWebComponent(ApiPost post, bool blocked, SelectionGroup? selectionGroup = null)
        {
            RedditPostWebComponent postComponent = new(post, _applicationHacks, _blockConfiguration, _configurationService, this, _visitTracker, Navigation, _redditClient, _applicationStyling, selectionGroup);
            return postComponent;
        }

        public SubRedditComponent CreateSubRedditComponent(SubRedditSubscription subscription, SelectionGroup? group = null)
        {
            return new SubRedditComponent(subscription, group is not null, this, _applicationStyling, group ?? new SelectionGroup());
        }

        public async Task<EmbeddedBrowser> OpenBrowser(PostItems resource)
        {
            EmbeddedBrowser browser = new(resource, _applicationStyling);
            await Navigation.PushAsync(browser);
            return browser;
        }

        public async Task<ReplyPage> OpenEditPage(ApiThing toEdit)
        {
            ReplyPage replyPage = new(null, toEdit, this, _redditClient, _applicationStyling);
            await Navigation.PushAsync(replyPage);
            return replyPage;
        }

        public async Task<EmbeddedImage> OpenImage(PostItems resource)
        {
            EmbeddedImage browser = new(_applicationStyling, resource);
            await Navigation.PushAsync(browser);
            return browser;
        }

        public async Task<SubRedditPage> OpenMessages(InboxSort sort = InboxSort.Unread)
        {
            SubRedditPage page = new(new ThingCollectionName("Messages", "/message", ThingKind.Message), sort, _applicationHacks, this, _redditClient, _applicationStyling, _blockConfiguration);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<ObjectEditorPage> OpenObjectEditor(object original)
        {
            ObjectEditorPage page = new(original, applicationTheme);
            await Navigation.PushAsync(page);
            return page;
        }

        public async Task OpenObjectEditor(Action onSave)
        {
            EditorConfiguration editorConfiguration = new()
            {
                BlockConfiguration = blockConfiguration,
                Credentials = _redditCredentials,
                Styling = applicationTheme,
                ApplicationHacks = applicationHacks
            };

            ObjectEditorPage editorPage = await this.OpenObjectEditor(editorConfiguration);

            editorPage.OnSave += (s, e) =>
            {
                _configurationService.Write(editorConfiguration.ApplicationHacks);
                _configurationService.Write(editorConfiguration.Credentials);
                _configurationService.Write(editorConfiguration.BlockConfiguration);
                _configurationService.Write(editorConfiguration.Styling);
            };

            onSave?.Invoke();
        }

        public async Task OpenObjectEditor()
        {
            await this.OpenObjectEditor(null);
        }

        public async Task<PostPage> OpenPost(ApiPost post, ApiComment focus)
        {
            PostPage postPage = new(post, focus, this, _configurationService, _redditClient, _applicationStyling, _applicationHacks, _blockConfiguration);
            await Navigation.PushAsync(postPage);
            postPage.TryLoad();
            return postPage;
        }

        public async Task<ReplyPage> OpenReplyPage(ApiThing comment)
        {
            ReplyPage replyPage = new(comment, null, this, _redditClient, _applicationStyling);
            await Navigation.PushAsync(replyPage);
            return replyPage;
        }

        public async Task<SubRedditPage> OpenSubReddit(string subRedditName, ApiPostSort sort = ApiPostSort.Hot)
        {
            return await this.OpenSubReddit(new ThingCollectionName(subRedditName), sort);
        }

        public async Task<SubRedditPage> OpenSubReddit(ThingCollectionName subRedditName, ApiPostSort sort = ApiPostSort.Hot)
        {
            SubRedditPage page = new(subRedditName, sort, _applicationHacks, this, _redditClient, _applicationStyling, _blockConfiguration);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<SubRedditAboutPage> OpenSubRedditAbout(ThingCollectionName subredditName)
        {
            SubRedditAboutPage page = new(subredditName, this, _redditClient, _applicationStyling);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<SubRedditPage> OpenUser(string username, UserProfileSort userProfileSort = UserProfileSort.New)
        {
            SubRedditPage page = new(new ThingCollectionName($"u/{username}"), userProfileSort, _applicationHacks, this, _redditClient, _applicationStyling, _blockConfiguration);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<EmbeddedVideo> OpenVideo(PostItems resource)
        {
            EmbeddedVideo browser = new(resource, _applicationStyling);
            await Navigation.PushAsync(browser);
            return browser;
        }
    }
}