using Deaddit.Components;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Pages.Models;
using Deaddit.Utils;

namespace Deaddit.Pages
{
    public partial class SubRedditAboutPage : ContentPage
    {
        private readonly ApplicationStyling _applicationTheme;

        private readonly IAppNavigator _appNavigator;

        private readonly IRedditClient _redditClient;

        private readonly SubRedditAboutPageModel _subRedditAboutPageModel;

        private readonly SubRedditName _subredditName;

        private ApiSubReddit? _apiSubReddit;

        public SubRedditAboutPage(SubRedditName subredditName, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _appNavigator = appNavigator;
            _applicationTheme = applicationTheme;
            _redditClient = redditClient;
            _subredditName = subredditName;

            BindingContext = _subRedditAboutPageModel = new SubRedditAboutPageModel(applicationTheme);
            this.InitializeComponent();
        }

        public async Task TryLoad()
        {
            await DataService.LoadAsync(mainStack, this.LoadAbout, _applicationTheme.HighlightColor.ToMauiColor());
        }

        private async Task LoadAbout()
        {
            _apiSubReddit = await _redditClient.About(_subredditName);

            _subRedditAboutPageModel.Description = _apiSubReddit.Description;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]

        public async void OnBackClicked(object? sender, object e)
        {
            await Navigation.PopAsync();
        }

        private async void OnHyperLinkClicked(object? sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);

            PostItems resource = RedditPostExtensions.Resolve(e.Url);

            await Navigation.OpenResource(resource, _appNavigator);
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
    }
}