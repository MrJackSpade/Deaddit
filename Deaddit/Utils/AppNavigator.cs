using Deaddit.Components;
using Deaddit.Components.WebComponents;
using Deaddit.Configurations;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Reddit;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.ThingDefinitions;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Validation;
using Deaddit.Interfaces;
using Deaddit.Pages;

namespace Deaddit.Utils
{
    /// <summary>
    /// This is a weird hack because the number of references that need to be passed around
    /// is way too large, and the number of redundant component inits is high enough
    /// that wonky fucking magic like this is the better option. Since components cant
    /// be reasonably unit tested to begin with, code cleanliness is actually better
    /// </summary>
    public class AppNavigator(IServiceProvider serviceProvider) : IAppNavigator
    {
        private readonly IServiceProvider _serviceProvider = Ensure.NotNull(serviceProvider);

        private IAggregatePostHandler AggregatePostHandler => _serviceProvider.GetService<IAggregatePostHandler>()!;

        private ApplicationHacks ApplicationHacks => _serviceProvider.GetService<ApplicationHacks>()!;

        private ApplicationStyling ApplicationStyling => _serviceProvider.GetService<ApplicationStyling>()!;

        private BlockConfiguration BlockConfiguration => _serviceProvider.GetService<BlockConfiguration>()!;

        private IConfigurationService ConfigurationService => _serviceProvider.GetService<IConfigurationService>()!;

        private IDisplayExceptions DisplayExceptions => _serviceProvider.GetService<IDisplayExceptions>()!;

        private INavigation Navigation => _serviceProvider.GetService<INavigation>()!;

        private IRedditClient RedditClient => _serviceProvider.GetService<IRedditClient>()!;

        private RedditCredentials RedditCredentials => _serviceProvider.GetService<RedditCredentials>()!;

        private ISelectBoxDisplay SelectBoxDisplay => _serviceProvider.GetService<ISelectBoxDisplay>()!;

        private IAggregateUrlHandler UrlHandler => _serviceProvider.GetService<IAggregateUrlHandler>()!;

        private IVisitTracker VisitTracker => _serviceProvider.GetService<IVisitTracker>()!;

        public RedditCommentWebComponent CreateCommentWebComponent(ApiComment comment, ApiPost? post = null, SelectionGroup? selectionGroup = null)
        {
            if (selectionGroup is null)
            {
                return new RedditCommentWebComponent(comment, post, false, SelectBoxDisplay, Navigation, this, RedditClient, ApplicationStyling, selectionGroup ?? new SelectionGroup(), BlockConfiguration);
            }
            else
            {
                return new RedditCommentWebComponent(comment, post, true, SelectBoxDisplay, Navigation, this, RedditClient, ApplicationStyling, selectionGroup ?? new SelectionGroup(), BlockConfiguration);
            }
        }

        public RedditMessageWebComponent CreateMessageWebComponent(ApiMessage message, SelectionGroup selectionGroup)
        {
            return new RedditMessageWebComponent(message, SelectBoxDisplay, Navigation, this, RedditClient, ApplicationStyling, selectionGroup, BlockConfiguration);
        }

        public MoreCommentsWebComponent CreateMoreCommentsWebComponent(IMore more)
        {
            return new MoreCommentsWebComponent(more, ApplicationStyling);
        }

        public RedditPostWebComponent CreatePostWebComponent(ApiPost post, PostState postHandling, SelectionGroup? selectionGroup = null)
        {
            RedditPostWebComponent postComponent = new(post, postHandling, SelectBoxDisplay, AggregatePostHandler, ApplicationHacks, BlockConfiguration, ConfigurationService, this, VisitTracker, Navigation, RedditClient, ApplicationStyling, selectionGroup);
            return postComponent;
        }

        public SubscriptionComponent CreateSubRedditComponent(ThingDefinition subscriptionThing, SelectionGroup? group = null)
        {
            return new SubscriptionComponent(subscriptionThing, group is not null, this, ApplicationStyling, group ?? new SelectionGroup());
        }

        public async Task<EmbeddedBrowser> OpenBrowser(string resource)
        {
            EmbeddedBrowser browser = new(resource, ApplicationStyling);
            await Navigation.PushAsync(browser);
            return browser;
        }

        public async Task<ReplyPage> OpenEditPage(ApiThing toEdit)
        {
            ReplyPage replyPage = new(null, toEdit, DisplayExceptions, this, RedditClient, ApplicationStyling);
            await Navigation.PushAsync(replyPage);
            return replyPage;
        }

        public async Task<EmbeddedImage> OpenImages(FileDownload[] resource)
        {
            EmbeddedImage browser = new(ApplicationStyling, resource);
            await Navigation.PushAsync(browser);
            return browser;
        }

        public async Task<ThingCollectionPage> OpenMessages(InboxSort sort = InboxSort.Unread)
        {
            ThingCollectionPage page = new(ThingDefinitionHelper.ForMessages(), sort, ApplicationHacks, DisplayExceptions, AggregatePostHandler, UrlHandler, this, RedditClient, ApplicationStyling, BlockConfiguration);
            await Navigation.PushAsync(page);
            await page.Init();
            return page;
        }

        public async Task<ObjectEditorPage> OpenObjectEditor(object original)
        {
            ObjectEditorPage page = new(original, ApplicationStyling);
            await Navigation.PushAsync(page);
            return page;
        }

        public async Task OpenObjectEditor(Action onSave)
        {
            EditorConfiguration editorConfiguration = new()
            {
                BlockConfiguration = BlockConfiguration,
                Credentials = RedditCredentials,
                Styling = ApplicationStyling,
                ApplicationHacks = ApplicationHacks
            };

            ObjectEditorPage editorPage = await this.OpenObjectEditor(editorConfiguration);

            editorPage.OnSave += (s, e) =>
            {
                ConfigurationService.Write(editorConfiguration.ApplicationHacks);
                ConfigurationService.Write(editorConfiguration.Credentials);
                ConfigurationService.Write(editorConfiguration.BlockConfiguration);
                ConfigurationService.Write(editorConfiguration.Styling);
            };

            onSave?.Invoke();
        }

        public async Task OpenObjectEditor()
        {
            await this.OpenObjectEditor(null);
        }

        public async Task<PostPage> OpenPost(ApiPost post, ApiComment focus)
        {
            PostPage postPage = new(post, focus, SelectBoxDisplay, DisplayExceptions, UrlHandler, AggregatePostHandler, this, ConfigurationService, RedditClient, ApplicationStyling, ApplicationHacks, BlockConfiguration);
            await Navigation.PushAsync(postPage);
            await postPage.TryLoad();
            return postPage;
        }

        public async Task<ReplyPage> OpenReplyPage(ApiThing comment)
        {
            ReplyPage replyPage = new(comment, null, DisplayExceptions, this, RedditClient, ApplicationStyling);
            await Navigation.PushAsync(replyPage);
            return replyPage;
        }

        public async Task<ThingCollectionPage> OpenSubReddit(string subRedditName, ApiPostSort sort = ApiPostSort.Hot)
        {
            ThingCollectionPage page = new(ThingDefinitionHelper.FromName(subRedditName), sort, ApplicationHacks, DisplayExceptions, AggregatePostHandler, UrlHandler, this, RedditClient, ApplicationStyling, BlockConfiguration);
            await Navigation.PushAsync(page);
            await page.Init();
            return page;
        }

        public async Task<SubRedditAboutPage> OpenSubRedditAbout(string subredditName)
        {
            SubRedditAboutPage page = new(ThingDefinitionHelper.ForSubReddit(subredditName), AggregatePostHandler, this, RedditClient, ApplicationStyling);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<ThingCollectionPage> OpenThing(ThingDefinition apiThing)
        {
            ThingCollectionPage page = new(apiThing, apiThing.DefaultSort, ApplicationHacks, DisplayExceptions, AggregatePostHandler, UrlHandler, this, RedditClient, ApplicationStyling, BlockConfiguration);
            await Navigation.PushAsync(page);
            await page.Init();
            return page;
        }

        public async Task<ThingCollectionPage> OpenUser(string username, UserProfileSort userProfileSort = UserProfileSort.New)
        {
            ThingCollectionPage page = new(ThingDefinitionHelper.ForUser(username), userProfileSort, ApplicationHacks, DisplayExceptions, AggregatePostHandler, UrlHandler, this, RedditClient, ApplicationStyling, BlockConfiguration);
            await Navigation.PushAsync(page);
            await page.Init();
            return page;
        }

        public async Task<EmbeddedVideo> OpenVideo(FileDownload url)
        {
            EmbeddedVideo browser = new(url, ApplicationStyling);
            await Navigation.PushAsync(browser);
            return browser;
        }
    }
}