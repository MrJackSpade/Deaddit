using Deaddit.Configurations;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.MAUI.Components;
using Deaddit.Pages.Models;

namespace Deaddit.Pages
{
    public partial class LandingPage : ContentPage
    {
        private readonly EditorConfiguration _appConfiguration;

        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly LandingPageConfiguration _configuration;

        private readonly IConfigurationService _configurationService;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly IVisitTracker _visitTracker;

        public LandingPage(ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, IRedditClient redditClient, RedditCredentials RedditCredentials, IConfigurationService configurationService, BlockConfiguration blockConfiguration)
        {
            //https://www.reddit.com/r/redditdev/comments/8pbx43/get_multireddit_listing/
            NavigationPage.SetHasNavigationBar(this, false);

            _redditClient = redditClient;

            _appConfiguration = new EditorConfiguration()
            {
                BlockConfiguration = blockConfiguration,
                Credentials = RedditCredentials,
                Styling = applicationTheme,
                ApplicationHacks = applicationHacks
            };

            _configuration = configurationService.Read<LandingPageConfiguration>();
            _configurationService = configurationService;
            _visitTracker = visitTracker;
            _blockConfiguration = blockConfiguration;
            _applicationTheme = applicationTheme;
            _selectionGroup = new SelectionGroup();

            BindingContext = new LandingPageViewModel(applicationTheme);
            this.InitializeComponent();

            mainStack.Add(SubRedditComponent.Fixed(new SubRedditSubscription("All", "r/all", ApiPostSort.Hot), redditClient, applicationTheme, applicationHacks, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService));
            mainStack.Add(SubRedditComponent.Fixed(new SubRedditSubscription("Home", "", ApiPostSort.Hot), redditClient, applicationTheme, applicationHacks, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService));

            foreach (SubRedditSubscription subscription in _configuration.Subscriptions)
            {
                SubRedditComponent subRedditComponent = SubRedditComponent.Removable(subscription, redditClient, applicationTheme, applicationHacks, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService);
                subRedditComponent.OnRemove += this.SubRedditComponent_OnRemove;
                mainStack.Add(subRedditComponent);
            }
        }

        public async void OnAddClicked(object? sender, EventArgs e)
        {
            string result = await this.DisplayPromptAsync("", "Enter a SubReddit");

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            if (!result.Contains('/'))
            {
                result = $"r/{result}";
            }

            SubRedditSubscription newSubscription = new()
            {
                SubReddit = result,
                DisplayString = result,
                Sort = ApiPostSort.Hot
            };

            _configuration.Subscriptions.Add(newSubscription);

            _configurationService.Write(_configuration);

            SubRedditComponent subRedditComponent = SubRedditComponent.Removable(newSubscription, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService);
            subRedditComponent.OnRemove += this.SubRedditComponent_OnRemove;
            mainStack.Add(subRedditComponent);
        }

        public void OnMenuClicked(object? sender, EventArgs e)
        {
            ObjectEditorPage editorPage = new(_appConfiguration, _applicationTheme);

            editorPage.OnSave += this.EditorPage_OnSave;

            Navigation.PushAsync(editorPage);
        }

        private void EditorPage_OnSave(object? sender, ObjectEditorSaveEventArgs e)
        {
            _configurationService.Write(_appConfiguration.ApplicationHacks);
            _configurationService.Write(_appConfiguration.Credentials);
            _configurationService.Write(_appConfiguration.BlockConfiguration);
            _configurationService.Write(_appConfiguration.Styling);
        }

        private void SubRedditComponent_OnRemove(object? sender, SubRedditSubscriptionRemoveEventArgs e)
        {
            mainStack.Remove(e.Component);
            _configuration.Subscriptions.Remove(e.Subscription);
            _configurationService.Write(_configuration);
        }
    }
}