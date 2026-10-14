using Deaddit.Components.WebComponents;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.ThingDefinitions;
using Reddit.Api.Models;
using Reddit.Api.Models.Json.Multis;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.MultiSelect;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Pages.Models;
using Deaddit.Utils;
using Maui.WebComponents.Extensions;
using System.Text.Json;

namespace Deaddit.Pages
{
    public partial class LandingPage : ContentPage
    {
        private const string BearerTokenPreferenceKey = "reddit_bearer_token";

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly LandingPageConfiguration _configuration;

        private readonly IConfigurationService _configurationService;

        private readonly IDisplayMessages _displayMessages;

        private readonly MultiSelector _multiselector;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup = new();

        private List<Multi> _multis = [];

        public LandingPage(IAppNavigator appNavigator, IDisplayMessages displayMessages, ISelectBoxDisplay selectBoxDisplay, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IRedditClient redditClient, IConfigurationService configurationService, BlockConfiguration blockConfiguration)
        {
            _applicationStyling = applicationTheme;
            //https://www.reddit.com/r/redditdev/comments/8pbx43/get_multireddit_listing/
            NavigationPage.SetHasNavigationBar(this, false);

            _appNavigator = appNavigator;
            _redditClient = redditClient;
            _displayMessages = displayMessages;
            _multiselector = new MultiSelector(selectBoxDisplay);

            _configuration = configurationService.Read<LandingPageConfiguration>();

            _configurationService = configurationService;

            BindingContext = new LandingPageViewModel(applicationTheme);
            this.InitializeComponent();

            webElement.SetColors(applicationTheme);
            webElement.OnJavascriptError += this.WebElement_OnJavascriptError;

            navigationBar.BackgroundColor = applicationTheme.PrimaryColor.ToMauiColor();

            _ = this.SafeInitializeContent();
        }

        private async Task SafeInitializeContent()
        {
            try
            {
                await DataService.LoadAsync(this.InitializeContent);
            }
            catch (Exception ex)
            {
                await _displayMessages.DisplayException(ex);
            }
        }

        public async void OnAddClicked(object? sender, EventArgs e)
        {
            try
            {
                if (_redditClient.IsLoggedIn)
                {
                    await _multiselector.Select(
                        "Add:",
                        new("Subreddit", this.AddSubreddit),
                        new("Multi", this.AddMulti));
                }
                else
                {
                    await this.AddSubreddit();
                }
            }
            catch (Exception ex)
            {
                _displayMessages.DisplayException(ex);
            }
        }

        private async Task AddSubreddit()
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

        private async Task AddMulti()
        {
            string name = await this.DisplayPromptAsync("", "Enter a name for the new multi");

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            Multi? multi = await _redditClient.CreateMulti(name);

            if (multi != null)
            {
                _multis.Add(multi);

                SubscriptionWebComponent component = _appNavigator.CreateSubscriptionWebComponent(
                    ThingDefinitionHelper.ForMulti(multi.Name),
                    _selectionGroup);

                Multi captured = multi;
                component.OnRemove += async (s, e) => await this.OnMultiRemove(captured, e);
                await webElement.AddChild(component);
            }
        }

        public async void OnMenuClicked(object? sender, EventArgs e)
        {
            await _appNavigator.OpenSettingsPage();
        }

        public async void OnMessageClicked(object sender, EventArgs e)
        {
            await _appNavigator.OpenMessages();
        }

        public async void OnProfileClicked(object? sender, EventArgs e)
        {
            if (_redditClient.LoggedInUser != null)
            {
                await _multiselector.Select(
                    "Profile:",
                    new($"/u/{_redditClient.LoggedInUser}", async () => await _appNavigator.OpenUser(_redditClient.LoggedInUser)),
                    new("Log Out", this.Logout));
            }
            else
            {
                await this.WebViewLogin();
            }
        }

        private async Task Logout()
        {
            Preferences.Remove(BearerTokenPreferenceKey);

#if ANDROID
            Android.Webkit.CookieManager.Instance?.RemoveAllCookies(null);
            Android.Webkit.CookieManager.Instance?.Flush();
#endif

            await this.DisplayAlert("Logged Out", "Restart the app to complete logout.", "OK");
        }

        /// <summary>
        /// Opens the browser login, resolves the token's owner and persists the resulting session
        /// (token, expiration and the browser's reddit.com cookies). Returns null if the login
        /// produced no usable token.
        /// </summary>
        private async Task<BearerToken?> LoginViaBrowser()
        {
            WebLogin? login = await _appNavigator.OpenRedditLogin();

            if (login == null)
            {
                return null;
            }

            string? owner = await _redditClient.GetTokenOwner(login.AccessToken);

            if (owner == null)
            {
                return null;
            }

            BearerToken session = new(login.AccessToken, owner, login.Expiration, login.Cookies);
            Preferences.Set(BearerTokenPreferenceKey, JsonSerializer.Serialize(session));

            return session;
        }

        private async Task<BearerToken?> RefreshToken()
        {
            // Session needed, expired or rejected: log in again and hand the inner client the
            // new session so it can (re-)send the request with it.
            try
            {
                return await this.LoginViaBrowser();
            }
            catch (Exception ex)
            {
                _displayMessages.DisplayException(ex);
                return null;
            }
        }

        private async Task WebViewLogin()
        {
            try
            {
                BearerToken? session = await this.LoginViaBrowser();

                if (session == null)
                {
                    return;
                }

                _redditClient.SetBearerToken(session);

                await this.LoadMultis();
            }
            catch (Exception ex)
            {
                _displayMessages.DisplayException(ex);
            }
        }

        private async Task InitializeContent()
        {
            _redditClient.SetTokenRefreshFunction(this.RefreshToken);

            await this.RestoreSession();

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

        private async Task RestoreSession()
        {
            string? json = Preferences.Get(BearerTokenPreferenceKey, null);

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            BearerToken? stored;
            try
            {
                stored = JsonSerializer.Deserialize<BearerToken>(json);
            }
            catch
            {
                Preferences.Remove(BearerTokenPreferenceKey);
                return;
            }

            if (stored == null)
            {
                Preferences.Remove(BearerTokenPreferenceKey);
                return;
            }

            // Sessions saved before cookies were captured can't load comments, and expired ones
            // aren't accepted anywhere. Renew them now, before the landing page loads, so it loads
            // logged in; the browser still holds the Reddit login, so the login page completes itself.
            if (stored.SessionCookies == null || stored.IsExpired)
            {
                Preferences.Remove(BearerTokenPreferenceKey);

                BearerToken? renewed = await this.LoginViaBrowser();

                if (renewed != null)
                {
                    _redditClient.SetBearerToken(renewed);
                }

                return;
            }

            if (await _redditClient.ValidateBearerToken(stored))
            {
                _redditClient.SetBearerToken(stored);
            }
            else
            {
                Preferences.Remove(BearerTokenPreferenceKey);
            }
        }

        private async Task LoadMultis()
        {
            if (!_redditClient.IsLoggedIn)
            {
                return;
            }

            try
            {
                await DataService.LoadAsync(webElement, async () =>
                {
                    _multis = await _redditClient.Multis();

                    foreach (Multi multi in _multis)
                    {
                        SubscriptionWebComponent component = _appNavigator.CreateSubscriptionWebComponent(
                            ThingDefinitionHelper.ForMulti(multi.Name),
                            _selectionGroup);

                        Multi captured = multi;
                        component.OnRemove += async (s, e) => await this.OnMultiRemove(captured, e);
                        await webElement.AddChild(component);
                    }
                }, _applicationStyling.HighlightColor.ToHex());
            }
            catch (Exception ex)
            {
                await _displayMessages.DisplayException(ex);
            }
        }

        private async Task OnMultiRemove(Multi multi, SubRedditSubscriptionRemoveEventArgs e)
        {
            bool confirmed = await this.DisplayAlert(
                "Delete Multi",
                $"Delete \"{multi.DisplayName}\"? This will permanently remove it from your Reddit account and cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            if (await _redditClient.DeleteMulti(multi))
            {
                await webElement.RemoveChild((SubscriptionWebComponent)e.Component);
                _multis.Remove(multi);
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