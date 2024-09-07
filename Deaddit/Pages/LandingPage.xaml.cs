using Deaddit.Configurations;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Extensions;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components;
using Deaddit.Pages.Models;
using Deaddit.Utils;

namespace Deaddit.Pages
{
    public partial class LandingPage : ContentPage
    {
        private readonly EditorConfiguration _appConfiguration;

        private readonly IAppNavigator _appNavigator;

        private readonly LandingPageConfiguration _configuration;

        private readonly IConfigurationService _configurationService;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup = new();

        public LandingPage(IAppNavigator appNavigator, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IRedditClient redditClient, RedditCredentials RedditCredentials, IConfigurationService configurationService, BlockConfiguration blockConfiguration)
        {
            //https://www.reddit.com/r/redditdev/comments/8pbx43/get_multireddit_listing/
            NavigationPage.SetHasNavigationBar(this, false);

            _appNavigator = appNavigator;
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

            BindingContext = new LandingPageViewModel(applicationTheme);
            this.InitializeComponent();

            mainStack.Add(_appNavigator.CreateSubRedditComponent(new SubRedditSubscription("All", "r/all", ApiPostSort.Hot), null));
            mainStack.Add(_appNavigator.CreateSubRedditComponent(new SubRedditSubscription("Home", "", ApiPostSort.Hot), null));
            mainStack.Add(_appNavigator.CreateSubRedditComponent(new SubRedditSubscription("Saved", "u/me/saved", ApiPostSort.Undefined), null));

            foreach (SubRedditSubscription subscription in _configuration.Subscriptions)
            {
                SubRedditComponent subRedditComponent = _appNavigator.CreateSubRedditComponent(subscription, _selectionGroup);
                subRedditComponent.OnRemove += this.SubRedditComponent_OnRemove;
                mainStack.Add(subRedditComponent);
            }

            DataService.LoadAsync(mainStack, this.LoadMultis, applicationTheme.HighlightColor.ToMauiColor());
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

            SubRedditComponent subRedditComponent = _appNavigator.CreateSubRedditComponent(newSubscription, _selectionGroup);
            subRedditComponent.OnRemove += this.SubRedditComponent_OnRemove;
            mainStack.Add(subRedditComponent);
        }

        public async void OnMenuClicked(object? sender, EventArgs e)
        {
            ObjectEditorPage editorPage = await _appNavigator.OpenObjectEditor(_appConfiguration);

            editorPage.OnSave += this.EditorPage_OnSave;
        }

        private void EditorPage_OnSave(object? sender, ObjectEditorSaveEventArgs e)
        {
            _configurationService.Write(_appConfiguration.ApplicationHacks);
            _configurationService.Write(_appConfiguration.Credentials);
            _configurationService.Write(_appConfiguration.BlockConfiguration);
            _configurationService.Write(_appConfiguration.Styling);
        }

        private async Task LoadMultis()
        {
            if (_redditClient.CanLogIn)
            {
                await foreach (ApiMulti multi in _redditClient.Multis())
                {
                    if (!multi.Subreddits.NotNullAny())
                    {
                        continue;
                    }

                    mainStack.Add(_appNavigator.CreateSubRedditComponent(
                                                    new SubRedditSubscription($"m/{multi.DisplayName}",
                                                                              $"m/{multi.Name}",
                                                                              ApiPostSort.Undefined),
                                                _selectionGroup));
                }
            }
        }

        private void SubRedditComponent_OnRemove(object? sender, SubRedditSubscriptionRemoveEventArgs e)
        {
            mainStack.Remove(e.Component);
            _configuration.Subscriptions.Remove(e.Subscription);
            _configurationService.Write(_configuration);
        }
    }
}