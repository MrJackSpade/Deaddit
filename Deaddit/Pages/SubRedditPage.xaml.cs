using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Extensions;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components;
using Deaddit.Pages.Models;
using Deaddit.Utils;
using System.Diagnostics;

namespace Deaddit.Pages
{
    public partial class SubRedditPage : ContentPage
    {
        private readonly ApplicationStyling _applicationTheme;

        private readonly IAppNavigator _appNavigator;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly List<LoadedThing> _loadedPosts = [];

        private readonly IRedditClient _redditClient;

        private readonly SemaphoreSlim _reloadSemaphore = new(1);

        /// <summary>
        /// Prevents more than one active loading/scrolling thread, because for some reason
        /// Monitor.Enter on a lock allows more than one thread to pass through.
        /// </summary>
        private readonly SemaphoreSlim _scrollSemaphore = new(1);

        private readonly SelectionGroup _selectionGroup;

        private readonly ThingCollectionName _thingCollectionName;

        private readonly ApplicationHacks _applicationHacks;

        private double _lastRefresh;

        private double _lastScroll;

        private Enum _sort;

        public SubRedditPage(ThingCollectionName subreddit, Enum sort, ApplicationHacks applicationHacks, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme, BlockConfiguration blockConfiguration)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _applicationHacks = Ensure.NotNull(applicationHacks);
            _appNavigator = Ensure.NotNull(appNavigator);
            _blockConfiguration = Ensure.NotNull(blockConfiguration);
            _thingCollectionName = Ensure.NotNull(subreddit);
            _sort = Ensure.NotNull(sort);
            _redditClient = Ensure.NotNull(redditClient);
            _applicationTheme = Ensure.NotNull(applicationTheme);

            _selectionGroup = new SelectionGroup();

            BindingContext = new SubRedditPageViewModel(subreddit);
            this.InitializeComponent();

            mainStack.Spacing = 1;
            mainStack.BackgroundColor = applicationTheme.SecondaryColor.ToMauiColor();

            navigationBar.BackgroundColor = applicationTheme.PrimaryColor.ToMauiColor();
            settingsButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            menuButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            subredditLabel.TextColor = applicationTheme.TextColor.ToMauiColor();
            subredditLabel.Text = subreddit.DisplayName;

            menuButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            reloadButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            infoButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            this.InitSortButtons(sort);
        }

        private void InitSortButtons(Enum sort)
        {
            sortButtons.Children.Clear();
            sortButtons.ColumnDefinitions.Clear();
            List<Enum> values = [];

            foreach (Enum e in Enum.GetValues(sort.GetType()))
            {
                if (Convert.ToInt32(e) != 0)
                {
                    values.Add(e);
                }
            }

            int columnCount = values.Count;

            // Define columns
            for (int i = 0; i < columnCount; i++)
            {
                sortButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            int column = 0;
            foreach (Enum sortValue in values)
            {
                Button button = new()
                {
                    Text = sortValue.ToString(),
                    BackgroundColor = Colors.Transparent,
                    TextColor = Colors.White,
                    FontSize = 14
                };

                button.Clicked += async (sender, e) =>
                {
                    _sort = sortValue;
                    await this.Reload();
                };

                sortButtons.Children.Add(button);
                Grid.SetColumn(button, column);
                column++;
            }
        }

        private bool WindowInLoadRange => scrollView.ScrollY >= scrollView.ContentSize.Height - scrollView.Height - navigationBar.Height;

        public async void OnInfoClicked(object? sender, EventArgs e)
        {
            await _appNavigator.OpenSubRedditAbout(_thingCollectionName);
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
            await _appNavigator.OpenObjectEditor();
        }

        public async Task ScrollDown(ScrolledEventArgs e)
        {
            int lastIndex = mainStack.Children.OfType<RedditPostComponent>().Count() - 1;

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

                ViewPosition position = scrollView.Position(child, _applicationTheme.ThumbnailSize * _applicationHacks.PageBuffer);

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

        private string? _after = null;

        public async Task TryLoad()
        {

            await DataService.LoadAsync(mainStack, async () =>
            {
                int newLoadedPostCount = 0;

                HashSet<string> loadedNames = _loadedPosts.Select(_loadedPosts => _loadedPosts.Post.Name).ToHashSet();

                List<ContentView> newComponents = [];

                do
                {

                    List<ApiThing> newPosts = await _redditClient.GetPosts(after: _after,
                                                                            subreddit: _thingCollectionName,
                                                                            sort: _sort,
                                                                            region: _applicationHacks.DefaultRegion)
                                                                           .Take(_applicationHacks.PageSize)
                                                                           .ToList();
                    Dictionary<string, UserPartial>? userData = null;

                    if (_blockConfiguration.RequiresUserData())
                    {
                        userData = await _redditClient.GetUserData(newPosts.Select(p => p.AuthorFullName).Distinct());
                    }

                    foreach (ApiThing thing in newPosts)
                    {
                        _after = thing.Name;

                        if (!_blockConfiguration.IsAllowed(thing, userData))
                        {
                            continue;
                        }

                        if(!loadedNames.Add(thing.Name))
                        {
                            continue;
                        }

                        ContentView? view = null;

                        if (thing is ApiPost post)
                        {
                            if (!string.IsNullOrWhiteSpace(post.Category))
                            {
                                Debug.WriteLine(post.Category);
                            }

                            if (post.ContentCategories.NotNullAny())
                            {
                                foreach (string category in post.ContentCategories)
                                {
                                    Debug.WriteLine(category);
                                }
                            }

                            if(post.Hidden)
                            {
                                continue;
                            }

                            RedditPostComponent redditPostComponent = _appNavigator.CreatePostComponent(post, _selectionGroup);

                            redditPostComponent.BlockAdded += this.RedditPostComponent_OnBlockAdded;
                            redditPostComponent.HideClicked += this.RedditPostComponent_HideClicked;

                            view = redditPostComponent;
                        }

                        if (thing is ApiComment comment)
                        {
                            view = _appNavigator.CreateCommentComponent(comment, null, _selectionGroup);
                        }

                        if (view is not null)
                        {
                            try
                            {
                                newComponents.Add(view);
                                newLoadedPostCount++;
                            }
                            catch (System.MissingMethodException mme) when (mme.Message.Contains("Microsoft.Maui.Controls.Handlers.Compatibility.FrameRenderer"))
                            {
                                Debug.WriteLine("FrameRenderer Missing Method Exception");
                            }

                            _loadedPosts.Add(new LoadedThing()
                            {
                                Post = thing,
                                PostComponent = view
                            });
                        }
                    }
                } while (newLoadedPostCount < _applicationHacks.PageSize);

                foreach (ContentView component in newComponents)
                {
                    mainStack.Children.Add(component);
                }
            }, _applicationTheme.HighlightColor.ToMauiColor());
        }

        private void RedditPostComponent_HideClicked(object? sender, OnHideClickedEventArgs e)
        {
            mainStack.Remove(e.Component);
        }

        private void RedditPostComponent_OnBlockAdded(object? sender, BlockRule e)
        {
            foreach (LoadedThing loadedPost in _loadedPosts.ToList())
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
            _after = null;

            //Cheap Hack
            IView nav = mainStack.Children.First();
            mainStack.Children.Clear();
            mainStack.Children.Add(nav);

            await this.TryLoad();
        }
    }
}