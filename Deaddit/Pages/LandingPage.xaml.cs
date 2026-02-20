using Deaddit.Components;
using Deaddit.Components.WebComponents.Partials;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Interfaces;
using Deaddit.Pages.Models;
using Deaddit.Utils;
using Maui.WebComponents.Components;
using Maui.WebComponents.Extensions;
using Reddit.Api;
using Reddit.Api.Extensions;
using Reddit.Api.Interfaces;
using Reddit.Api.Models;
using Reddit.Api.Models.Api;
using Reddit.Api.Models.ThingDefinitions;

namespace Deaddit.Pages
{
    public partial class LandingPage : ContentPage
    {
        private readonly IAppNavigator _appNavigator;

        private readonly ApplicationStyling _applicationStyling;

        private readonly LandingPageConfiguration _configuration;

        private readonly IConfigurationService _configurationService;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup = new();

        public LandingPage(IAppNavigator appNavigator, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IRedditClient redditClient, RedditCredentials RedditCredentials, IConfigurationService configurationService, BlockConfiguration blockConfiguration)
        {
            _applicationStyling = applicationTheme;
            //https://www.reddit.com/r/redditdev/comments/8pbx43/get_multireddit_listing/
            NavigationPage.SetHasNavigationBar(this, false);

            _appNavigator = appNavigator;
            _redditClient = redditClient;

            _configuration = configurationService.Read<LandingPageConfiguration>();

            _configurationService = configurationService;

            BindingContext = new LandingPageViewModel(applicationTheme);
            this.InitializeComponent();

            mainStack.Add(_appNavigator.CreateSubRedditComponent(ThingDefinitionHelper.ForSubReddit("All"), null));
            mainStack.Add(_appNavigator.CreateSubRedditComponent(new HomeDefinition(), null));
            mainStack.Add(_appNavigator.CreateSubRedditComponent(new SavedDefinition(), null));
            mainStack.Add(_appNavigator.CreateHistoryComponent());

            foreach (SubRedditSubscription subscription in _configuration.Subscriptions)
            {
                ThingDefinition thingDefinition = ThingDefinitionHelper.FromName(subscription.ThingName);
                SubscriptionComponent subRedditComponent = _appNavigator.CreateSubRedditComponent(thingDefinition, _selectionGroup);
                subRedditComponent.OnRemove += this.SubRedditComponent_OnRemove;
                mainStack.Add(subRedditComponent);
            }

            DataService.LoadAsync(this.LoadMultisWithSpinner);
        }

        private async Task LoadMultisWithSpinner()
        {
            RedditWebElement spinnerContainer = new() { HeightRequest = 50 };
            WebComponent activityIndicator = new Spinner(_applicationStyling.HighlightColor.ToHex());

            mainStack.Add(spinnerContainer);
            await spinnerContainer.AddChild(activityIndicator);

            await this.LoadMultis();

            mainStack.Remove(spinnerContainer);
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
                ThingName = result,
                DisplayString = result,
                Sort = ApiPostSort.Hot
            };

            _configuration.Subscriptions.Add(newSubscription);

            _configurationService.Write(_configuration);

            ThingDefinition thingDefinition = ThingDefinitionHelper.FromName(newSubscription.ThingName);

            SubscriptionComponent subRedditComponent = _appNavigator.CreateSubRedditComponent(thingDefinition, _selectionGroup);
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
                                                ThingDefinitionHelper.ForMulti(multi.Name),
                                                _selectionGroup));
                }
            }
        }

        private void SubRedditComponent_OnRemove(object? sender, SubRedditSubscriptionRemoveEventArgs e)
        {
            mainStack.Remove(e.Component);

            foreach (SubRedditSubscription subscription in _configuration.Subscriptions)
            {
                ThingDefinition thingDefinition = ThingDefinitionHelper.FromName(subscription.ThingName);

                if (thingDefinition.Equals(e.Thing))
                {
                    _configuration.Subscriptions.Remove(subscription);
                    break;
                }
            }

            _configurationService.Write(_configuration);
        }
    }
}