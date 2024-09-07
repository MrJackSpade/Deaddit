﻿using Deaddit.Core.Configurations.Interfaces;
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
    public class AppNavigator(IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService) : IAppNavigator
    {
        private readonly ApplicationHacks _applicationHacks = Ensure.NotNull(applicationHacks);

        private readonly ApplicationStyling _applicationTheme = Ensure.NotNull(applicationTheme);

        private readonly BlockConfiguration _blockConfiguration = Ensure.NotNull(blockConfiguration);

        private readonly IConfigurationService _configurationService = Ensure.NotNull(configurationService);

        private readonly IRedditClient _redditClient = Ensure.NotNull(redditClient);

        private readonly IVisitTracker _visitTracker = Ensure.NotNull(visitTracker);

        private static INavigation Navigation => Shell.Current.Navigation;

        public RedditCommentComponent CreateCommentComponent(ApiComment comment, ApiPost? post = null, SelectionGroup? selectionGroup = null)
        {
            if (selectionGroup is null)
            {
                return new RedditCommentComponent(comment, post, false, this, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, selectionGroup ?? new SelectionGroup(), _blockConfiguration, _configurationService);
            }
            else
            {
                return new RedditCommentComponent(comment, post, true, this, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, selectionGroup ?? new SelectionGroup(), _blockConfiguration, _configurationService);
            }
        }

        public MoreCommentsComponent CreateMoreCommentsComponent(ApiMore more)
        {
            return new MoreCommentsComponent(more, _applicationTheme);
        }

        public RedditPostComponent CreatePostComponent(ApiPost post, SelectionGroup? selectionGroup = null)
        {
            RedditPostComponent postComponent = new(post, selectionGroup is not null, this, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, selectionGroup ?? new SelectionGroup(), _blockConfiguration, _configurationService);
            return postComponent;
        }

        public SubRedditComponent CreateSubRedditComponent(SubRedditSubscription subscription, SelectionGroup group)
        {
            return new SubRedditComponent(subscription, group is not null, this, _applicationTheme, group ?? new SelectionGroup());
        }

        public async Task<EmbeddedBrowser> OpenBrowser(PostItems resource)
        {
            EmbeddedBrowser browser = new(resource, _applicationTheme);
            await Navigation.PushAsync(browser);
            return browser;
        }

        public async Task<EmbeddedImage> OpenImage(PostItems resource)
        {
            EmbeddedImage browser = new(_applicationTheme, resource);
            await Navigation.PushAsync(browser);
            return browser;
        }

        public async Task<ObjectEditorPage> OpenObjectEditor(object original)
        {
            ObjectEditorPage page = new(original, applicationTheme);

            await Navigation.PushAsync(page);

            return page;
        }

        public async Task<PostPage> OpenPost(ApiPost post, ApiComment focus)
        {
            PostPage postPage = new(post, focus, this, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration);
            await Navigation.PushAsync(postPage);
            await postPage.TryLoad();
            return postPage;
        }

        public async Task<ReplyPage> OpenReplyPage(ApiThing comment)
        {
            ReplyPage replyPage = new(comment, this, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
            await Navigation.PushAsync(replyPage);
            return replyPage;
        }

        public async Task<SubRedditPage> OpenSubReddit(string subRedditName, ApiPostSort sort = ApiPostSort.Hot)
        {
            return await this.OpenSubReddit(new SubRedditName(subRedditName), sort);
        }

        public async Task<SubRedditPage> OpenSubReddit(SubRedditName subRedditName, ApiPostSort sort = ApiPostSort.Hot)
        {
            SubRedditPage page = new(subRedditName, sort, this, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<SubRedditPage> OpenUser(string username, UserProfileSort userProfileSort = UserProfileSort.New)
        {
            SubRedditPage page = new(new SubRedditName($"u/{username}"), userProfileSort, this, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<SubRedditAboutPage> OpenSubRedditAbout(SubRedditName subredditName)
        {
            SubRedditAboutPage page = new(subredditName, this, _redditClient, _applicationTheme);
            await Navigation.PushAsync(page);
            await page.TryLoad();
            return page;
        }

        public async Task<EmbeddedVideo> OpenVideo(PostItems resource)
        {
            EmbeddedVideo browser = new(resource, _applicationTheme);
            await Navigation.PushAsync(browser);
            return browser;
        }
    }
}