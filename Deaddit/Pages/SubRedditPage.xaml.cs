using Deaddit.Components;
using Deaddit.Interfaces;
using Deaddit.Models.Json.Response;
using Deaddit.PageModels;
using Deaddit.Services;

namespace Deaddit.Pages
{
    public partial class SubRedditPage : ContentPage
    {
        private readonly IAppTheme _appTheme;

        private readonly List<RedditPost> _loadedPosts = [];

        /// <summary>
        /// Prevents more than one active loading/scrolling thread, because for some reason
        /// Monitor.Enter on a lock allows more than one thread to pass through.
        /// </summary>
        private readonly SemaphoreSlim _loadSemaphore = new(1);

        private readonly IMarkDownService _markDownService;

        private readonly IRedditClient _redditClient;

        private readonly SelectionTracker _selectionTracker;

        private readonly string _subreddit;

        private string _sort;

        public SubRedditPage(string subreddit, string sort, IRedditClient redditClient, IAppTheme appTheme, IMarkDownService markDownService)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _subreddit = subreddit;
            _sort = sort;
            _redditClient = redditClient;
            _appTheme = appTheme;
            _markDownService = markDownService;
            _selectionTracker = new SelectionTracker();

            BindingContext = new SubRedditPageViewModel(subreddit, appTheme);
            this.InitializeComponent();

            mainStack.Spacing = 1;
            mainStack.BackgroundColor = appTheme.SecondaryColor;

            navigationBar.BackgroundColor = appTheme.PrimaryColor;
            settingsButton.TextColor = appTheme.TextColor;
            menuButton.TextColor = appTheme.TextColor;
        }

        public async void OnMenuClicked(object sender, EventArgs e)
        {
        }

        public async void OnSettingsClicked(object sender, EventArgs e)
        {
        }

        public async void ScrollView_Scrolled(object sender, ScrolledEventArgs e)
        {
            if (scrollView.ScrollY >= (scrollView.ContentSize.Height - scrollView.Height - navigationBar.Height))
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
                List<RedditPost> posts = [];

                string? after = _loadedPosts.LastOrDefault()?.Name;

                await foreach (RedditPost post in _redditClient.Read(after: after, subreddit: _subreddit, sort: _sort))
                {
                    posts.Add(post);

                    if (posts.Count >= 25)
                    {
                        break;
                    }
                }

                _loadedPosts.AddRange(posts);

                foreach (RedditPost post in posts)
                {
                    RedditPostComponent redditPostComponent = RedditPostComponent.ListView(post, _redditClient, _appTheme, _selectionTracker, _markDownService);

                    mainStack.Add(redditPostComponent);
                }
            }, _appTheme.HighlightColor);
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

        private async Task Reload()
        {
            _loadedPosts.Clear();

            //Cheap Hack
            var nav = this.mainStack.Children.First();
            this.mainStack.Children.Clear();
            this.mainStack.Children.Add(nav);

            await this.TryLoad();
        }
    }
}