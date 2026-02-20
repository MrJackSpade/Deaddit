using Deaddit.Components.WebComponents;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Pages.Models;
using Deaddit.Utils;
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

        private readonly IDisplayMessages _displayMessages;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup = new();

        public LandingPage(IAppNavigator appNavigator, IDisplayMessages displayMessages, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IRedditClient redditClient, RedditCredentials RedditCredentials, IConfigurationService configurationService, BlockConfiguration blockConfiguration)
        {
            _applicationStyling = applicationTheme;
            //https://www.reddit.com/r/redditdev/comments/8pbx43/get_multireddit_listing/
            NavigationPage.SetHasNavigationBar(this, false);

            _appNavigator = appNavigator;
            _redditClient = redditClient;
            _displayMessages = displayMessages;

            _configuration = configurationService.Read<LandingPageConfiguration>();

            _configurationService = configurationService;

            BindingContext = new LandingPageViewModel(applicationTheme);
            this.InitializeComponent();

            webElement.SetColors(applicationTheme);
            webElement.OnJavascriptError += this.WebElement_OnJavascriptError;

            navigationBar.BackgroundColor = applicationTheme.PrimaryColor.ToMauiColor();

            DataService.LoadAsync(this.InitializeContent);
        }

        private async Task InitializeContent()
        {
            await webElement.AddChild(_appNavigator.CreateSubscriptionWebComponent(ThingDefinitionHelper.ForSubReddit("All"), null));
            await webElement.AddChild(_appNavigator.CreateSubscriptionWebComponent(new HomeDefinition(), null));
            await webElement.AddChild(_appNavigator.CreateSubscriptionWebComponent(new SavedDefinition(), null));
            await webElement.AddChild(_appNavigator.CreateHistoryWebComponent());

            foreach (SubRedditSubscription subscription in _configuration.Subscriptions)
            {
                ThingDefinition thingDefinition = ThingDefinitionHelper.FromName(subscription.ThingName);
                SubscriptionWebComponent subRedditComponent = _appNavigator.CreateSubscriptionWebComponent(thingDefinition, _selectionGroup);
                subRedditComponent.OnRemove += this.SubRedditComponent_OnRemove;
                await webElement.AddChild(subRedditComponent);
            }

            await this.LoadMultis();
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

            SubscriptionWebComponent subRedditComponent = _appNavigator.CreateSubscriptionWebComponent(thingDefinition, _selectionGroup);
            subRedditComponent.OnRemove += this.SubRedditComponent_OnRemove;
            await webElement.AddChild(subRedditComponent);
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
                await DataService.LoadAsync(webElement, async () =>
                {
                    foreach (ApiMulti multi in await _redditClient.Multis())
                    {
                        if (!multi.Subreddits.NotNullAny())
                        {
                            continue;
                        }

                        await webElement.AddChild(_appNavigator.CreateSubscriptionWebComponent(
                                                    ThingDefinitionHelper.ForMulti(multi.Name),
                                                    _selectionGroup));
                    }
                }, _applicationStyling.HighlightColor.ToHex());
            }
        }

        private async void SubRedditComponent_OnRemove(object? sender, SubRedditSubscriptionRemoveEventArgs e)
        {
            await webElement.RemoveChild((SubscriptionWebComponent)e.Component);

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

        private void WebElement_OnJavascriptError(object? sender, Exception e)
        {
            _displayMessages.DisplayException(e);
        }
    }
}
