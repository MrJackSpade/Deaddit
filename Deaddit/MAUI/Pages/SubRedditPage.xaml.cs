using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.MAUI.Components;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Services;
using Deaddit.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

        private readonly SubRedditName _subreddit;

        private readonly IVisitTracker _visitTracker;

        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members")]
        private Thread? _loadThread = null;

        private ApiPostSort _sort;

        public SubRedditPage(SubRedditName subreddit, ApiPostSort sort, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            NavigationPage.SetHasNavigationBar(this, false);

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

            subredditLabel.TextColor = applicationTheme.TextColor;
            subredditLabel.Text = subreddit.DisplayName;

            hotButton.TextColor = applicationTheme.TextColor;
            controversialButton.TextColor = applicationTheme.TextColor;
            newButton.TextColor = applicationTheme.TextColor;
            risingButton.TextColor = applicationTheme.TextColor;
            topButton.TextColor = applicationTheme.TextColor;
            menuButton.TextColor = applicationTheme.TextColor;
            reloadButton.TextColor = applicationTheme.TextColor;
            infoButton.TextColor = applicationTheme.TextColor;
        }

        private bool WindowInLoadRange => scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - navigationBar.Height;

        public async void OnInfoClicked(object? sender, EventArgs e)
        {
            SubRedditAboutPage page = new(_subreddit, _redditClient, _applicationTheme);
            await page.TryLoad();
            await Navigation.PushAsync(page);
        }

        public async void OnMenuClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public async void OnReloadClicked(object? sender, EventArgs e)
        {
            await this.Reload();
        }

        public async void OnSettingsClicked(object? sender, EventArgs e)
        {
            _loadedPosts.Clear();
            await this.TryLoad();
        }

        public async void ScrollView_Scrolled(object? sender, ScrolledEventArgs e)
        {
            if (_loadSemaphore.Wait(0))
            {
                if (WindowInLoadRange)
                {
                    //_loadThread = new(async () =>
                    //{
                    await this.TryLoad();

                    _loadThread = null;

                    _loadSemaphore.Release();
                    //});

                    //_loadThread.Start();
                }
                else
                {
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

                HashSet<string> loadedNames = _loadedPosts.Select(_loadedPosts => _loadedPosts.Post.Name).ToHashSet();

                await foreach (ApiPost post in _redditClient.GetPosts(after: after, subreddit: _subreddit, sort: _sort))
                {
                    after = post.Name;

                    if (!_blockConfiguration.BlockRules.IsAllowed(post))
                    {
                        continue;
                    }

                    if (post.Hidden)
                    {
                        continue;
                    }

                    if (loadedNames.Add(post.Name))
                    {
                        posts.Add(post);
                    }

                    if (posts.Count >= 25)
                    {
                        break;
                    }
                }

                foreach (ApiPost post in posts)
                {
                    RedditPostComponent redditPostComponent = RedditPostComponent.ListView(post, _redditClient, _applicationTheme, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService);

                    redditPostComponent.BlockAdded += this.RedditPostComponent_OnBlockAdded;
                    redditPostComponent.HideClicked += this.RedditPostComponent_HideClicked;

                    try
                    {
                        mainStack.Add(redditPostComponent);
                    }
                    catch (System.MissingMethodException mme) when (mme.Message.Contains("Microsoft.Maui.Controls.Handlers.Compatibility.FrameRenderer"))
                    {
                        Debug.WriteLine("FrameRenderer Missing Method Exception");
                    }

                    _loadedPosts.Add(new LoadedPost()
                    {
                        Post = post,
                        PostComponent = redditPostComponent
                    });
                }
            }, _applicationTheme.HighlightColor);
        }

        private async void OnControversialClicked(object? sender, EventArgs e)
        {
            _sort = ApiPostSort.Controversial;
            await this.Reload();
        }

        private async void OnHotClicked(object? sender, EventArgs e)
        {
            _sort = ApiPostSort.Hot;
            await this.Reload();
        }

        private async void OnNewClicked(object? sender, EventArgs e)
        {
            _sort = ApiPostSort.New;
            await this.Reload();
        }

        private async void OnRisingClicked(object? sender, EventArgs e)
        {
            _sort = ApiPostSort.Rising;
            await this.Reload();
        }

        private async void OnTopClicked(object? sender, EventArgs e)
        {
            _sort = ApiPostSort.Top;
            await this.Reload();
        }

        private void RedditPostComponent_HideClicked(object? sender, OnHideClickedEventArgs e)
        {
            mainStack.Remove(e.Component);
        }

        private void RedditPostComponent_OnBlockAdded(object? sender, BlockRule e)
        {
            foreach (LoadedPost loadedPost in _loadedPosts.ToList())
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

            public VisualElement PostComponent { get; set; }
        }
    }
}