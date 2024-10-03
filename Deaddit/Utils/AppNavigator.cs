﻿using Deaddit.Components.WebComponents;
using Deaddit.Configurations;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
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
    public class AppNavigator(IServiceProvider serviceProvider) : IAppNavigator
    {
        private readonly IServiceProvider _serviceProvider = Ensure.NotNull(serviceProvider);

        private static INavigation Navigation => Shell.Current.Navigation;

        private IAggregatePostHandler ApiPostHandler => _serviceProvider.GetService<IAggregatePostHandler>()!;

        private ApplicationHacks ApplicationHacks => _serviceProvider.GetService<ApplicationHacks>()!;

        private ApplicationStyling ApplicationStyling => _serviceProvider.GetService<ApplicationStyling>()!;

        private BlockConfiguration BlockConfiguration => _serviceProvider.GetService<BlockConfiguration>()!;

        private IConfigurationService ConfigurationService => _serviceProvider.GetService<IConfigurationService>()!;

        private IRedditClient RedditClient => _serviceProvider.GetService<IRedditClient>()!;

        private RedditCredentials RedditCredentials => _serviceProvider.GetService<RedditCredentials>()!;

        private IAggregateUrlHandler UrlHandler => _serviceProvider.GetService<IAggregateUrlHandler>()!;

        private IVisitTracker VisitTracker => _serviceProvider.GetService<IVisitTracker>()!;

        public RedditCommentWebComponent CreateCommentWebComponent(ApiComment comment, ApiPost? post = null, SelectionGroup? selectionGroup = null)
        {
            if (selectionGroup is null)
            {
                return new RedditCommentWebComponent(comment, post, false, Navigation, this, RedditClient, ApplicationStyling, selectionGroup ?? new SelectionGroup(), BlockConfiguration);
            }
            else
            {
                return new RedditCommentWebComponent(comment, post, true, Navigation, this, RedditClient, ApplicationStyling, selectionGroup ?? new SelectionGroup(), BlockConfiguration);
            }
        }

        public RedditMessageWebComponent CreateMessageWebComponent(ApiComment message)
        {
            throw new NotImplementedException();
        }

        public MoreCommentsWebComponent CreateMoreCommentsWebComponent(IMore more)
        {
            return new MoreCommentsWebComponent(more, ApplicationStyling);
        }

        public RedditPostWebComponent CreatePostWebComponent(ApiPost post, bool blocked, SelectionGroup? selectionGroup = null)
        {
            RedditPostWebComponent postComponent = new(post, blocked, ApiPostHandler, ApplicationHacks, BlockConfiguration, ConfigurationService, this, VisitTracker, Navigation, RedditClient, ApplicationStyling, selectionGroup);
            return postComponent;
        }

        public SubRedditComponent CreateSubRedditComponent(SubRedditSubscription subscription, SelectionGroup? group = null)
        {
            return new SubRedditComponent(subscription, group is not null, this, ApplicationStyling, group ?? new SelectionGroup());
        }

        public async Task<EmbeddedBrowser> OpenBrowser(string resource)
        {
            EmbeddedBrowser browser = new(resource, ApplicationStyling);
            await Navigation.PushAsync(browser);
            return browser;
        }

        public async Task<ReplyPage> OpenEditPage(ApiThing toEdit)
        {
            ReplyPage replyPage = new(null, toEdit, this, RedditClient, ApplicationStyling);
            await Navigation.PushAsync(replyPage);
            return replyPage;
        }

        public async Task<EmbeddedImage> OpenImages(FileDownload[] resource)
        {
            EmbeddedImage browser = new(ApplicationStyling, resource);
            await Navigation.PushAsync(browser);
            return browser;
        }

        public async Task<SubRedditPage> OpenMessages(InboxSort sort = InboxSort.Unread)
        {
            SubRedditPage page = new(new ThingCollectionName("Messages", "/message", ThingKind.Message), sort, ApplicationHacks, ApiPostHandler, UrlHandler, this, RedditClient, ApplicationStyling, BlockConfiguration);
            await Navigation.PushAsync(page);
            await page.TryLoad();
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
            PostPage postPage = new(post, focus, UrlHandler, ApiPostHandler, this, ConfigurationService, RedditClient, ApplicationStyling, ApplicationHacks, BlockConfiguration);
            await Navigation.PushAsync(postPage);
            postPage.TryLoad();
            return postPage;
        }

        public async Task<ReplyPage> OpenReplyPage(ApiThing comment)
        {
            ReplyPage replyPage = new(comment, null, this, RedditClient, ApplicationStyling);
            await Navigation.PushAsync(replyPage);
            return replyPage;
        }

        public async Task<SubRedditPage> OpenSubReddit(string subRedditName, ApiPostSort sort = ApiPostSort.Hot)
        {
            return await this.OpenSubReddit(new ThingCollectionName(subRedditName), sort);
        }

        public async Task<SubRedditPage> OpenSubReddit(ThingCollectionName subRedditName, ApiPostSort sort = ApiPostSort.Hot)
        {
            SubRedditPage page = new(subRedditName, sort, ApplicationHacks, ApiPostHandler, UrlHandler, this, RedditClient, ApplicationStyling, BlockConfiguration);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<SubRedditAboutPage> OpenSubRedditAbout(ThingCollectionName subredditName)
        {
            SubRedditAboutPage page = new(subredditName, this, RedditClient, ApplicationStyling);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<SubRedditPage> OpenUser(string username, UserProfileSort userProfileSort = UserProfileSort.New)
        {
            SubRedditPage page = new(new ThingCollectionName($"u/{username}"), userProfileSort, ApplicationHacks, ApiPostHandler, UrlHandler, this, RedditClient, ApplicationStyling, BlockConfiguration);
            await Navigation.PushAsync(page);
            await page.TryLoad();
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