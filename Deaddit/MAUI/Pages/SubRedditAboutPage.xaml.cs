using Deaddit.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.MAUI.Components;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Services;
using Deaddit.Utils;

namespace Deaddit.MAUI.Pages
{
    public partial class SubRedditAboutPage : ContentPage
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly IRedditClient _redditClient;

        private readonly SubRedditAboutPageModel _subRedditAboutPageModel;

        private readonly string _subredditName;

        private ApiSubReddit _apiSubReddit;

        public SubRedditAboutPage(string subredditName, IRedditClient redditClient, ApplicationTheme applicationTheme)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _applicationTheme = applicationTheme;
            _redditClient = redditClient;
            _subredditName = subredditName;

            BindingContext = _subRedditAboutPageModel = new SubRedditAboutPageModel(applicationTheme);
            this.InitializeComponent();
        }

        public async Task TryLoad()
        {
            await DataService.LoadAsync(mainStack, this.LoadAbout, _applicationTheme.HighlightColor);
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

        private async void OnBackClicked(object sender, object e)
        {
            await Navigation.PopAsync();
        }

        private async void OnHyperLinkClicked(object sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);

            PostTarget resource = UrlHandler.Resolve(e.Url);

            await Navigation.OpenResource(resource, _redditClient, _applicationTheme, null, null, null);
        }

        private void OnMoreClicked(object sender, object e)
        {
        }

        private void OnRulesClicked(object sender, object e)
        {
        }

        private async void OnSubscribeClicked(object sender, object e)
        {
            await _redditClient.ToggleSubScription(_apiSubReddit, !_apiSubReddit.UserIsSubscriber);
            _apiSubReddit.UserIsSubscriber = !_apiSubReddit.UserIsSubscriber;
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