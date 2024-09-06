using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.MAUI.Components;
using Deaddit.Pages.Models;
using Deaddit.Utils;
using System.Diagnostics;

namespace Deaddit.Pages
{
    public partial class SubRedditPage : ContentPage
    {
        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private readonly List<LoadedPost> _loadedPosts = [];

        /// <summary>
        /// Prevents more than one active loading/scrolling thread, because for some reason
        /// Monitor.Enter on a lock allows more than one thread to pass through.
        /// </summary>
        private readonly SemaphoreSlim _scrollSemaphore = new(1);

        private readonly IRedditClient _redditClient;

        private readonly SemaphoreSlim _reloadSemaphore = new(1);

        private readonly SelectionGroup _selectionGroup;

        private readonly SubRedditName _subreddit;

        private readonly IVisitTracker _visitTracker;

        private ApiPostSort _sort;

        public SubRedditPage(SubRedditName subreddit, ApiPostSort sort, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _visitTracker = visitTracker;
            _configurationService = configurationService;
            _blockConfiguration = blockConfiguration;
            _subreddit = subreddit;
            _sort = sort;
            _redditClient = redditClient;
            _applicationHacks = applicationHacks;
            _applicationTheme = applicationTheme;

            _selectionGroup = new SelectionGroup();

            BindingContext = new SubRedditPageViewModel(subreddit, applicationTheme);
            this.InitializeComponent();

            mainStack.Spacing = 1;
            mainStack.BackgroundColor = applicationTheme.SecondaryColor.ToMauiColor();

            navigationBar.BackgroundColor = applicationTheme.PrimaryColor.ToMauiColor();
            settingsButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            menuButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            subredditLabel.TextColor = applicationTheme.TextColor.ToMauiColor();
            subredditLabel.Text = subreddit.DisplayName;

            hotButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            controversialButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            newButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            risingButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            topButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            menuButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            reloadButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            infoButton.TextColor = applicationTheme.TextColor.ToMauiColor();
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
            if (_reloadSemaphore.Wait(0))
            {
                await this.Reload();
                _reloadSemaphore.Release();
            }
        }

        public async void OnSettingsClicked(object? sender, EventArgs e)
        {
            _loadedPosts.Clear();
            await this.TryLoad();
        }

        private double _lastScroll;

        public async Task ScrollDown(ScrolledEventArgs e)
        {

            int lastIndex = mainStack.Children.Count - 1;

            if (WindowInLoadRange)
            {
                await this.TryLoad();
            }
            else if (Math.Abs(_lastRefresh - e.ScrollY) < _applicationTheme.ThumbnailSize)
            {
                return;
            }

            _lastRefresh = e.ScrollY;

            List<RedditPostComponent> children = [.. mainStack.Children.OfType<RedditPostComponent>()];

            for (int i = lastIndex - 1; i >= 0; i--)
            {
                RedditPostComponent child = children[i];

                ViewPosition viewPosition = scrollView.Position(child, scrollView.Height);

                switch (viewPosition)
                {
                    case ViewPosition.Above:
                        if (!child.Deinitialize())
                        {
                            return;
                        }

                        break;
                    case ViewPosition.Below:
                    case ViewPosition.Unknown:
                        continue;
                    case ViewPosition.Within:
                        child.Initialize();
                        break;
                }
            }
        }

        private double _lastRefresh;

        public void ScrollUp(ScrolledEventArgs e)
        {
            if (Math.Abs(_lastRefresh - e.ScrollY) < _applicationTheme.ThumbnailSize)
            {
                return;
            }

            _lastRefresh = e.ScrollY;

            List<RedditPostComponent> children = [.. mainStack.Children.OfType<RedditPostComponent>()];

            for (int i = children.Count - 1; i >= 0; i--)
            {
                RedditPostComponent child = children[i];

                ViewPosition position = scrollView.Position(child, scrollView.Height);

                switch (position)
                {
                    case ViewPosition.Above:
                        return;
                    case ViewPosition.Below:
                        child.Deinitialize();
                        break;
                    case ViewPosition.Within:
                        child.Initialize();
                        break;
                }
            }
        }

        public async void ScrollView_Scrolled(object? sender, ScrolledEventArgs e)
        {
            if (_scrollSemaphore.Wait(0))
            {

                if (e.ScrollY < _lastScroll)
                {
                    this.ScrollUp(e);
                }
                else
                {
                    await this.ScrollDown(e);
                }

                _lastScroll = e.ScrollY;

                _scrollSemaphore.Release();
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
                    RedditPostComponent redditPostComponent = RedditPostComponent.ListView(post, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService);

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
            }, _applicationTheme.HighlightColor.ToMauiColor());
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