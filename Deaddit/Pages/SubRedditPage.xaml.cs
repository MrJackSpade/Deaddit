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
        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly List<LoadedThing> _loadedPosts = [];

        private readonly SemaphoreSlim _loadsemaphore = new(1);

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly Grid _sortButtons;

        private readonly ThingCollectionName _thingCollectionName;

        private string? _after = null;

        private Enum _sort;

        public SubRedditPage(ThingCollectionName subreddit, Enum sort, ApplicationHacks applicationHacks, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme, BlockConfiguration blockConfiguration)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            if(subreddit.Kind == ThingKind.Account)
            {
                _isBlockEnabled = false;
                blockButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            }

            _applicationHacks = Ensure.NotNull(applicationHacks);
            _appNavigator = Ensure.NotNull(appNavigator);
            _blockConfiguration = Ensure.NotNull(blockConfiguration);
            _thingCollectionName = Ensure.NotNull(subreddit);
            _sort = Ensure.NotNull(sort);
            _redditClient = Ensure.NotNull(redditClient);
            _applicationStyling = Ensure.NotNull(applicationTheme);
            _sortButtons = [];
            _selectionGroup = new SelectionGroup();

            BindingContext = new SubRedditPageViewModel(subreddit);

            this.InitializeComponent();

            scrollView.Add(_sortButtons, false);
            scrollView.Spacing = 1;
            scrollView.BackgroundColor = applicationTheme.SecondaryColor.ToMauiColor();
            scrollView.ScrolledDown = this.ScrollDown;
            scrollView.HeaderCount = 1;

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
            if (_loadsemaphore.Wait(0))
            {
                await this.Reload();
                _loadsemaphore.Release();
            }
        }

        public async void OnSettingsClicked(object? sender, EventArgs e)
        {
            await _appNavigator.OpenObjectEditor();
        }

        public async Task ScrollDown(ScrolledEventArgs e)
        {
            if (WindowInLoadRange)
            {
                if (_loadsemaphore.Wait(0))
                {
                    await this.TryLoad();
                    _loadsemaphore.Release();
                }
            }
        }

        public async Task TryLoad()
        {
            // Wrap the loading process in a DataService call, likely for UI updates or error handling
            await DataService.LoadAsync(scrollView.InnerStack, async () =>
            {
                // Create a set of already loaded post names to avoid duplicates
                HashSet<string> loadedNames = _loadedPosts.Select(_loadedPosts => _loadedPosts.Post.Name).ToHashSet();

                List<ContentView> newComponents = [];

                do
                {
                    // Fetch new posts from Reddit API
                    List<ApiThing> newPosts = await _redditClient.GetPosts(after: _after,
                                                                            subreddit: _thingCollectionName,
                                                                            sort: _sort,
                                                                            region: _applicationHacks.DefaultRegion)
                                                                           .Take(_applicationHacks.PageSize)
                                                                           .ToList();

                    // Break if no new posts are fetched
                    if (newPosts.Count == 0)
                    {
                        break;
                    }

                    Dictionary<string, UserPartial>? userData = null;

                    // Fetch user data if required by block configuration
                    if (_blockConfiguration.RequiresUserData() &&
                       //Cheap hack. Don't pull user data if this is an account page because we don't filter
                       //based on account, on account pages. That wouldn't make sense.
                       _thingCollectionName.Kind != ThingKind.Account)
                    {
                        userData = await _redditClient.GetUserData(newPosts.Select(p => p.AuthorFullName).Distinct());
                    }

                    foreach (ApiThing thing in newPosts)
                    {
                        // Update the 'after' cursor for pagination
                        _after = thing.Name;

                        bool blocked = !_blockConfiguration.IsAllowed(thing, userData);

                        // Skip if not allowed by block configuration
                        if (blocked && _isBlockEnabled)
                        {
                            continue;
                        }

                        // Skip if already loaded
                        if (!loadedNames.Add(thing.Name))
                        {
                            continue;
                        }

                        ContentView? view = null;

                        // Handle ApiPost
                        if (thing is ApiPost post)
                        {
                            // Debug logging for categories
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

                            // Skip hidden posts
                            if (post.Hidden)
                            {
                                continue;
                            }

                            // Create post component
                            RedditPostComponent redditPostComponent = _appNavigator.CreatePostComponent(post, blocked, _selectionGroup);

                            // Attach event handlers
                            redditPostComponent.BlockAdded += this.RedditPostComponent_OnBlockAdded;
                            redditPostComponent.HideClicked += this.RedditPostComponent_HideClicked;

                            view = redditPostComponent;
                        }

                        // Handle ApiComment
                        if (thing is ApiComment comment)
                        {
                            view = _appNavigator.CreateCommentComponent(comment, null, _selectionGroup);
                        }

                        // Add view to new components if created
                        if (view is not null)
                        {
                            try
                            {
                                newComponents.Add(view);
                            }
                            catch (System.MissingMethodException mme) when (mme.Message.Contains("Microsoft.Maui.Controls.Handlers.Compatibility.FrameRenderer"))
                            {
                                Debug.WriteLine("FrameRenderer Missing Method Exception");
                            }

                            // Add to loaded posts
                            _loadedPosts.Add(new LoadedThing()
                            {
                                Post = thing,
                                PostComponent = view
                            });
                        }
                    }
                } while (newComponents.Count < _applicationHacks.PageSize);

                // Add all new components to the scroll view
                foreach (ContentView component in newComponents)
                {
                    scrollView.Add(component);
                }
            }, _applicationStyling.HighlightColor.ToMauiColor());
        }

        private bool _isBlockEnabled = true;

        public async void OnBlockClicked(object? sender, EventArgs e)
        {
            _isBlockEnabled = !_isBlockEnabled;

            if (_isBlockEnabled)
            {
                blockButton.TextColor = Color.Parse("#FF0000");
            } else
            {
                blockButton.TextColor = _applicationStyling.TextColor.ToMauiColor();
            }

            await this.Reload();
        }

        private void InitSortButtons(Enum sort)
        {
            _sortButtons.Children.Clear();
            _sortButtons.ColumnDefinitions.Clear();
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
                _sortButtons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
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

                _sortButtons.Children.Add(button);
                Grid.SetColumn(button, column);
                column++;
            }
        }

        private void RedditPostComponent_HideClicked(object? sender, OnHideClickedEventArgs e)
        {
            scrollView.Remove(e.Component);
        }

        private void RedditPostComponent_OnBlockAdded(object? sender, BlockRule e)
        {
            foreach (LoadedThing loadedPost in _loadedPosts.ToList())
            {
                if (!e.IsAllowed(loadedPost.Post))
                {
                    scrollView.Remove(loadedPost.PostComponent);
                    _loadedPosts.Remove(loadedPost);
                }
            }
        }

        private async Task Reload()
        {
            _loadedPosts.Clear();
            _after = null;

            //Cheap Hack
            scrollView.Clear();
            scrollView.Add(_sortButtons);

            await this.TryLoad();
        }
    }
}