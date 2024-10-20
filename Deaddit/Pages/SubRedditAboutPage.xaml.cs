using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.ThingDefinitions;
using Deaddit.Interfaces;
using Deaddit.Pages.Models;
using Deaddit.Utils;
using Maui.WebComponents.Components;
using Maui.WebComponents.Extensions;

namespace Deaddit.Pages
{
    public partial class SubRedditAboutPage : ContentPage
    {
        private readonly IAggregatePostHandler _aggregatePostHandler;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly DivComponent _bodyDiv;

        private readonly IRedditClient _redditClient;

        private readonly SubRedditAboutPageModel _subRedditAboutPageModel;

        private readonly SubRedditDefinition _subredditName;

        private readonly DivComponent _wrapperDiv;

        private ApiSubReddit? _apiSubReddit;

        public SubRedditAboutPage(SubRedditDefinition subredditName, IAggregatePostHandler aggregatePostHandler, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _appNavigator = appNavigator;
            _applicationStyling = applicationTheme;
            _redditClient = redditClient;
            _aggregatePostHandler = aggregatePostHandler;
            _subredditName = subredditName;
            BindingContext = _subRedditAboutPageModel = new SubRedditAboutPageModel(applicationTheme);

            this.InitializeComponent();

            _bodyDiv = new();
            _wrapperDiv = new();
            _wrapperDiv.Children.Add(_bodyDiv);
            webElement.AddChild(_wrapperDiv);
            webElement.ClickUrl += this.WebElement_ClickUrl;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async void OnBackClicked(object? sender, object e)
        {
            await Navigation.PopAsync();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public void OnMoreClicked(object? sender, object e)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public void OnRulesClicked(object? sender, object e)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async void OnSubscribeClicked(object? sender, object e)
        {
            await _redditClient.ToggleSubScription(_apiSubReddit, !_apiSubReddit.UserIsSubscriber);
            _apiSubReddit.UserIsSubscriber = !_apiSubReddit.UserIsSubscriber;
            this.SetSubscribeButtonState(_apiSubReddit.UserIsSubscriber);
        }

        public async Task TryLoad()
        {
            await DataService.LoadAsync(_wrapperDiv, this.LoadAbout, _applicationStyling.HighlightColor.ToHex());
        }

        private async Task LoadAbout()
        {
            webElement.SetColors(_applicationStyling);

            _apiSubReddit = await _redditClient.About(_subredditName);

            _bodyDiv.InnerHTML = _apiSubReddit.DescriptionHtml ?? string.Empty;

            _subRedditAboutPageModel.Name = _apiSubReddit.DisplayName;

            if (!string.IsNullOrWhiteSpace(_apiSubReddit.IconImg))
            {
                _subRedditAboutPageModel.Thumbnail = _apiSubReddit.IconImg;
            }
            else if (!string.IsNullOrWhiteSpace(_apiSubReddit.CommunityIcon))
            {
                _subRedditAboutPageModel.Thumbnail = _apiSubReddit.CommunityIcon;
            }

            _subRedditAboutPageModel.VisibleMetaData = $"{_apiSubReddit.Subscribers} subscribers, {_apiSubReddit.ActiveUserCount} online";

            this.SetSubscribeButtonState(_apiSubReddit.UserIsSubscriber);
        }

        private void SetSubscribeButtonState(bool state)
        {
            if (state)
            {
                subscribeButton.Text = "Unsubscribe";
            }
            else
            {
                subscribeButton.Text = "Subscribe";
            }
        }

        private async void WebElement_ClickUrl(object? sender, string e)
        {
            if (!_aggregatePostHandler.UrlHandler.CanLaunch(e, _aggregatePostHandler))
            {
                await this.DisplayAlert("Alert", $"Can not handle url {e}", "OK");
                return;
            }

            await _aggregatePostHandler.UrlHandler.Launch(e, _aggregatePostHandler);
        }
    }
}