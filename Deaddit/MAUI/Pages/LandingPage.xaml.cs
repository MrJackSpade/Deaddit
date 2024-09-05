using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.MAUI.Components;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Utils;

namespace Deaddit.MAUI.Pages
{
    public partial class LandingPage : ContentPage
    {
        private readonly AppConfiguration _appConfiguration;

        private readonly ApplicationTheme _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly LandingPageConfiguration _configuration;

        private readonly IConfigurationService _configurationService;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly IVisitTracker _visitTracker;

        public LandingPage(ApplicationTheme applicationTheme, IVisitTracker visitTracker, IRedditClient redditClient, RedditCredentials RedditCredentials, IConfigurationService configurationService, BlockConfiguration blockConfiguration)
        {
            //https://www.reddit.com/r/redditdev/comments/8pbx43/get_multireddit_listing/
            NavigationPage.SetHasNavigationBar(this, false);

            _redditClient = redditClient;

            _appConfiguration = new AppConfiguration()
            {
                BlockConfiguration = blockConfiguration,
                Credentials = RedditCredentials,
                Theme = applicationTheme
            };

            _configuration = configurationService.Read<LandingPageConfiguration>();
            _configurationService = configurationService;
            _visitTracker = visitTracker;
            _blockConfiguration = blockConfiguration;
            _applicationTheme = applicationTheme;
            _selectionGroup = new SelectionGroup();

            BindingContext = new LandingPageViewModel(applicationTheme);
            this.InitializeComponent();

            mainStack.Add(SubRedditComponent.Fixed(new SubRedditSubscription("All", "r/all", ApiPostSort.Hot), redditClient, applicationTheme, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService));
            mainStack.Add(SubRedditComponent.Fixed(new SubRedditSubscription("Home", "", ApiPostSort.Hot), redditClient, applicationTheme, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService));

            foreach (SubRedditSubscription subscription in _configuration.Subscriptions)
            {
                SubRedditComponent subRedditComponent = SubRedditComponent.Removable(subscription, redditClient, applicationTheme, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService);
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

            SubRedditComponent subRedditComponent = SubRedditComponent.Removable(newSubscription, _redditClient, _applicationTheme, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService);
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
            _configurationService.Write(_appConfiguration.Credentials);
            _configurationService.Write(_appConfiguration.BlockConfiguration);
            _configurationService.Write(_appConfiguration.Theme);
        }

        private void SubRedditComponent_OnRemove(object? sender, SubRedditSubscriptionRemoveEventArgs e)
        {
            mainStack.Remove(e.Component);
            _configuration.Subscriptions.Remove(e.Subscription);
            _configurationService.Write(_configuration);
        }
    }
}