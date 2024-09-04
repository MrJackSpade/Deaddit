using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.MAUI.Components;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models.Api;
using Deaddit.Services;
using Deaddit.Utils;

namespace Deaddit.MAUI.Pages
{
    public partial class SubRedditPage : ContentPage
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private readonly List<LoadedPost> _loadedPosts = [];

        /// <summary>
        /// Prevents more than one active loading/scrolling thread, because for some reason
        /// Monitor.Enter on a lock allows more than one thread to pass through.
        /// </summary>
        private readonly SemaphoreSlim _loadSemaphore = new(1);

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly string _subreddit;

        private readonly IVisitTracker _visitTracker;

        private string _sort;

        public async void OnInfoClicked(object sender,  EventArgs e)
        {
            await _redditClient.About(_subreddit);
        }

        public SubRedditPage(string subreddit, string sort, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            if (!subreddit.Contains('/'))
            {
                subreddit = "/r/" + subreddit;
            }

            _visitTracker = visitTracker;
            _configurationService = configurationService;
            _blockConfiguration = blockConfiguration;
            _subreddit = subreddit;
            _sort = sort;
            _redditClient = redditClient;
            _applicationTheme = applicationTheme;

            _selectionGroup = new SelectionGroup();

            BindingContext = new SubRedditPageViewModel(subreddit, applicationTheme);
            this.InitializeComponent();

            mainStack.Spacing = 1;
            mainStack.BackgroundColor = applicationTheme.SecondaryColor;

            navigationBar.BackgroundColor = applicationTheme.PrimaryColor;
            settingsButton.TextColor = applicationTheme.TextColor;
            menuButton.TextColor = applicationTheme.TextColor;
        }

        public async void OnMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public async void OnReloadClicked(object sender, EventArgs e)
        {
            await this.Reload();
        }

        public async void OnSettingsClicked(object sender, EventArgs e)
        {
            _loadedPosts.Clear();
            await this.TryLoad();
        }

        public async void ScrollView_Scrolled(object sender, ScrolledEventArgs e)
        {
            if (scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - navigationBar.Height)
            {
                if (_loadSemaphore.Wait(0))
                {
                    await this.TryLoad();

                    _loadSemaphore.Release();
                }
            }
        }

        public async Task TryLoad()
        {
            await DataService.LoadAsync(mainStack, async () =>
            {
                List<ApiPost> posts = [];

                string? after = _loadedPosts.LastOrDefault()?.Post.Name;

                await foreach (ApiPost post in _redditClient.Read(after: after, subreddit: _subreddit, sort: _sort))
                {
                    if (!_blockConfiguration.BlockRules.IsAllowed(post))
                    {
                        continue;
                    }

                    posts.Add(post);

                    if (posts.Count >= 25)
                    {
                        break;
                    }
                }

                foreach (ApiPost post in posts)
                {
                    RedditPostComponent redditPostComponent = RedditPostComponent.ListView(post, _redditClient, _applicationTheme, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService);

                    redditPostComponent.OnBlockAdded += this.RedditPostComponent_OnBlockAdded;

                    mainStack.Add(redditPostComponent);

                    _loadedPosts.Add(new LoadedPost()
                    {
                        Post = post,
                        PostComponent = redditPostComponent
                    });
                }
            }, _applicationTheme.HighlightColor);
        }

        private async void OnControversialClicked(object sender, EventArgs e)
        {
            _sort = "Controversial";
            await this.Reload();
        }

        private async void OnHotClicked(object sender, EventArgs e)
        {
            _sort = "Hot";
            await this.Reload();
        }

        private async void OnNewClicked(object sender, EventArgs e)
        {
            _sort = "New";
            await this.Reload();
        }

        private async void OnRisingClicked(object sender, EventArgs e)
        {
            _sort = "Rising";
            await this.Reload();
        }

        private async void OnTopClicked(object sender, EventArgs e)
        {
            _sort = "Top";
            await this.Reload();
        }

        private void RedditPostComponent_OnBlockAdded(object? sender, BlockRule e)
        {
            foreach (LoadedPost loadedPost in _loadedPosts)
            {
                if (!e.IsAllowed(loadedPost.Post))
                {
                    mainStack.Remove(loadedPost.PostComponent);
                    _loadedPosts.Remove(loadedPost);
                }
            }
        }

        private async Task Reload()
        {
            _loadedPosts.Clear();

            //Cheap Hack
            IView nav = mainStack.Children.First();
            mainStack.Children.Clear();
            mainStack.Children.Add(nav);

            await this.TryLoad();
        }

        private class LoadedPost
        {
            public ApiPost Post { get; set; }

            public RedditPostComponent PostComponent { get; set; }
        }
    }
}