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

            DataService.LoadAsync(this.LoadMultis);
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
            await _appNavigator.OpenObjectEditor();
        }

        public async void OnMessageClicked(object sender, EventArgs e)
        {
            await _appNavigator.OpenMessages();
        }

        private async Task LoadMultis()
        {
            if (_redditClient.CanLogIn)
            {
                foreach (ApiMulti multi in await _redditClient.Multis())
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