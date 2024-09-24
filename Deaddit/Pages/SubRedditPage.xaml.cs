﻿using Deaddit.Components.WebComponents;
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
using Deaddit.Pages.Models;
using Deaddit.Utils;
using Maui.WebComponents.Components;
using System.Diagnostics;
using Maui.WebComponents.Extensions;

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

        private bool _isBlockEnabled = true;

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
            _applicationStyling = Ensure.NotNull(applicationTheme);
            _sortButtons = [];
            _selectionGroup = new SelectionGroup();

            BindingContext = new SubRedditPageViewModel(subreddit);

            this.InitializeComponent();

            webElement.BodyStyle["background-color"] = applicationTheme.SecondaryColor.ToHex();

            if (subreddit.Kind == ThingKind.Account)
            {
                _isBlockEnabled = false;
                blockButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            }

            webElement.OnScrollBottom += this.ScrollDown;

            navigationBar.BackgroundColor = applicationTheme.PrimaryColor.ToMauiColor();
            settingsButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            subredditLabel.TextColor = applicationTheme.TextColor.ToMauiColor();
            subredditLabel.Text = subreddit.DisplayName;

            reloadButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            infoButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            this.InitSortButtons(sort);
        }

        public async void OnBlockClicked(object? sender, EventArgs e)
        {
            _isBlockEnabled = !_isBlockEnabled;

            if (_isBlockEnabled)
            {
                blockButton.TextColor = Color.Parse("#FF0000");
            }
            else
            {
                blockButton.TextColor = _applicationStyling.TextColor.ToMauiColor();
            }

            await this.Reload();
        }

        public async void OnInfoClicked(object? sender, EventArgs e)
        {
            await _appNavigator.OpenSubRedditAbout(_thingCollectionName);
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

        public async void ScrollDown(object? sender, EventArgs e)
        {
            if (_loadsemaphore.Wait(0))
            {
                await this.TryLoad();
                _loadsemaphore.Release();
            }
        }

        public async Task TryLoad()
        {
            // Wrap the loading process in a DataService call
            await DataService.LoadAsync(null, async () =>
            {
                // Create a set of already loaded post names to avoid duplicates
                HashSet<string> loadedNames = _loadedPosts.Select(_loadedPosts => _loadedPosts.Post.Name).ToHashSet();

                List<WebComponent> newComponents = [];

                do
                {
                    // Fetch new posts from Reddit API
                    List<ApiThing> newPosts = await _redditClient.GetPosts(after: _after,
                                                                            subreddit: _thingCollectionName,
                                                                            pageSize: _applicationHacks.PageSize,
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

                        WebComponent? view = null;

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
                            RedditPostWebComponent redditPostComponent = _appNavigator.CreatePostWebComponent(post, blocked, _selectionGroup);

                            // Attach event handlers
                            redditPostComponent.BlockAdded += this.RedditPostComponent_OnBlockAdded;
                            redditPostComponent.HideClicked += this.RedditPostComponent_HideClicked;

                            view = redditPostComponent;
                        }

                        // Handle ApiComment
                        if (thing is ApiComment comment)
                        {
                            //view = _appNavigator.CreateCommentComponent(comment, null, _selectionGroup);
                        }

                        if (thing is ApiMessage message)
                        {
                            //view = _appNavigator.CreateMessageComponent(message, _selectionGroup);
                        }

                        // Add view to new components if created
                        if (view is null)
                        {
                            throw new NotImplementedException();
                        }

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
                } while (newComponents.Count < _applicationHacks.PageSize);

                // Add all new components to the scroll view
                foreach (WebComponent component in newComponents)
                {
                    await webElement.AddChild(component);
                }
            }, _applicationStyling.HighlightColor.ToMauiColor());
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

                if (sort.Equals(sortValue))
                {
                    button.BackgroundColor = _applicationStyling.TertiaryColor.ToMauiColor();
                }

                button.Clicked += async (sender, e) =>
                {
                    _sort = sortValue;
                    this.UpdateSort(sortValue);
                    await this.Reload();
                };

                _sortButtons.Children.Add(button);
                Grid.SetColumn(button, column);
                column++;
            }
        }

        private async void RedditPostComponent_HideClicked(object? sender, OnHideClickedEventArgs e)
        {
            await webElement.RemoveChild(e.Component);
        }

        private async void RedditPostComponent_OnBlockAdded(object? sender, BlockRule e)
        {
            foreach (LoadedThing loadedPost in _loadedPosts.ToList())
            {
                if (!e.IsAllowed(loadedPost.Post))
                {
                    await webElement.RemoveChild(loadedPost.PostComponent);
                    _loadedPosts.Remove(loadedPost);
                }
            }
        }

        private async Task Reload()
        {
            _loadedPosts.Clear();
            _after = null;

            await webElement.Clear();

            await this.TryLoad();
        }

        private void UpdateSort(Enum sort)
        {
            foreach (Button button in _sortButtons.Children.Cast<Button>())
            {
                if (button.Text == sort.ToString())
                {
                    button.BackgroundColor = _applicationStyling.TertiaryColor.ToMauiColor();
                }
                else
                {
                    button.BackgroundColor = Colors.Transparent;
                }
            }
        }
    }
}