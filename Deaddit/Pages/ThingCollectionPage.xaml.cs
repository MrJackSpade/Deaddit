﻿using Deaddit.Components.WebComponents;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Interfaces;
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
using Maui.WebComponents.Extensions;
using System.Diagnostics;

namespace Deaddit.Pages
{
    public partial class ThingCollectionPage : ContentPage
    {
        private readonly IAggregatePostHandler _aggregatePostHandler;

        private readonly IAggregateUrlHandler _aggregateUrlHandler;

        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IDisplayExceptions _displayExceptions;

        private readonly List<LoadedThing> _loadedPosts = [];

        private readonly SemaphoreSlim _loadsemaphore = new(1);

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly DivComponent _sortButtons = new();

        private readonly ThingCollectionName _thingCollectionName;

        private string? _after = null;

        private bool _isBlockEnabled = true;

        private Enum _sort;

        public ThingCollectionPage(ThingCollectionName subreddit, Enum sort, ApplicationHacks applicationHacks, IDisplayExceptions displayExceptions, IAggregatePostHandler aggregatePostHandler, IAggregateUrlHandler aggregateUrlHandler, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling, BlockConfiguration blockConfiguration)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _applicationHacks = Ensure.NotNull(applicationHacks);
            _appNavigator = Ensure.NotNull(appNavigator);
            _blockConfiguration = Ensure.NotNull(blockConfiguration);
            _thingCollectionName = Ensure.NotNull(subreddit);
            _sort = Ensure.NotNull(sort);
            _redditClient = Ensure.NotNull(redditClient);
            _applicationStyling = Ensure.NotNull(applicationStyling);
            _aggregatePostHandler = Ensure.NotNull(aggregatePostHandler);
            _aggregateUrlHandler = Ensure.NotNull(aggregateUrlHandler);
            _displayExceptions = Ensure.NotNull(displayExceptions);

            _selectionGroup = new SelectionGroup();
            _sortButtons = new DivComponent()
            {
                Display = "flex",
                FlexDirection = "row",
                Width = "100%",
                BackgroundColor = "red"
            };

            BindingContext = new SubRedditPageViewModel(subreddit);

            this.InitializeComponent();

            webElement.SetBackgroundColor(applicationStyling.SecondaryColor);
            webElement.SetBlockQuoteColor(applicationStyling.TextColor);
            webElement.SetSpoilerColor(applicationStyling.TextColor);
            webElement.ClickUrl += this.WebElement_ClickUrl;
            webElement.OnJavascriptError += this.WebElement_OnJavascriptError;

            if (subreddit.Kind == ThingKind.Account)
            {
                _isBlockEnabled = false;
                blockButton.TextColor = applicationStyling.TextColor.ToMauiColor();
            }

            webElement.OnScrollBottom += this.ScrollDown;

            navigationBar.BackgroundColor = applicationStyling.PrimaryColor.ToMauiColor();
            settingsButton.TextColor = applicationStyling.TextColor.ToMauiColor();

            subredditLabel.TextColor = applicationStyling.TextColor.ToMauiColor();
            subredditLabel.Text = subreddit.DisplayName;

            reloadButton.TextColor = applicationStyling.TextColor.ToMauiColor();
            infoButton.TextColor = applicationStyling.TextColor.ToMauiColor();

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
            await DataService.LoadAsync(this.webElement, async () =>
            {
                // Create a set of already loaded post names to avoid duplicates
                HashSet<string> loadedNames = _loadedPosts.Select(_loadedPosts => _loadedPosts.Post.Name).ToHashSet();

                HashSet<string> loadedTitles = _loadedPosts.Select(p => p.Post.GetTitleOrEmpty())
                                                           .Where(s => !string.IsNullOrWhiteSpace(s))
                                                           .ToHashSet()!;

                HashSet<string> loadedUrls = _loadedPosts.Select(p => p.Post.GetUrlOrEmpty())
                                                           .Where(s => !string.IsNullOrWhiteSpace(s))
                                                           .ToHashSet()!;

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

                        PostState postState = this.GetPostState(loadedTitles, loadedUrls, userData, thing);

                        if (postState == PostState.Block && _isBlockEnabled)
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
                            RedditPostWebComponent redditPostComponent = _appNavigator.CreatePostWebComponent(post, postState, _selectionGroup);

                            // Attach event handlers
                            redditPostComponent.BlockAdded += this.RedditPostComponent_OnBlockAdded;
                            redditPostComponent.HideClicked += this.RedditPostComponent_HideClicked;

                            loadedTitles.Add(post.GetTitleOrEmpty());
                            loadedUrls.Add(post.GetUrlOrEmpty());

                            view = redditPostComponent;
                        }

                        // Handle ApiComment
                        if (thing is ApiComment comment)
                        {
                            view = _appNavigator.CreateCommentWebComponent(comment, null, _selectionGroup);
                        }

                        if (thing is ApiMessage message)
                        {
                            view = _appNavigator.CreateMessageWebComponent(message, _selectionGroup);
                        }

                        // Add view to new components if created
                        if (view is null)
                        {
                            throw new NotImplementedException();
                        }

                        newComponents.Add(view);

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
            }, _applicationStyling.HighlightColor.ToHex());
        }

        protected override bool OnBackButtonPressed()
        {
            if (_loadedPosts.Count < _applicationHacks.PageSize * 2)
            {
                return false;
            }

            //Why does this not pop if there are more than 2 pages in the stack?
            if (Navigation.NavigationStack.Count > 2)
            {
                return false;
            }

            Task<bool> answer = this.DisplayAlert("Confirm", "Do you want to exit?", "Yes", "No");

            answer.ContinueWith(async task =>
            {
                if (task.Result)
                {
                    await Navigation.PopAsync();
                }
            });

            return true;
        }

        private PostState GetPostState(HashSet<string> loadedTitles, HashSet<string> loadedUrls, Dictionary<string, UserPartial>? userData, ApiThing thing)
        {
            bool blocked = !_blockConfiguration.IsAllowed(thing, userData);

            // Skip if not allowed by block configuration
            if (blocked)
            {
                return PostState.Block;
            }

            if (_blockConfiguration.DuplicateTitleHandling == PostState.Block &&
                loadedTitles.Contains(thing.GetTitleOrEmpty().Trim(), StringComparer.OrdinalIgnoreCase))
            {
                return PostState.Block;
            }

            if (_blockConfiguration.DuplicateLinkHandling == PostState.Block &&
                loadedUrls.Contains(thing.GetUrlOrEmpty()))
            {
                return PostState.Block;
            }

            // Code looks weird but these come last as a set because block takes precedence
            if (_blockConfiguration.DuplicateTitleHandling == PostState.Visited &&
                loadedTitles.Contains(thing.GetTitleOrEmpty().Trim(), StringComparer.OrdinalIgnoreCase))
            {
                return PostState.Visited;
            }

            if (_blockConfiguration.DuplicateLinkHandling == PostState.Visited &&
                loadedUrls.Contains(thing.GetUrlOrEmpty()))
            {
                return PostState.Visited;
            }

            return PostState.None;
        }

        private async Task InitSortButtons(Enum sort)
        {
            await webElement.AddChild(_sortButtons);

            _sortButtons.Children.Clear();

            List<Enum> values = [];

            foreach (Enum e in Enum.GetValues(sort.GetType()))
            {
                if (Convert.ToInt32(e) != 0)
                {
                    values.Add(e);
                }
            }

            foreach (Enum sortValue in values)
            {
                ButtonComponent button = new()
                {
                    InnerText = sortValue.ToString(),
                    Color = _applicationStyling.TextColor.ToHex(),
                    FontSize = $"{_applicationStyling.TitleFontSize}px",
                    BackgroundColor = _applicationStyling.SecondaryColor.ToHex(),
                    Border = "0",
                    FlexGrow = "1",
                    Padding = "15px",
                };

                if (sort.Equals(sortValue))
                {
                    button.BackgroundColor = _applicationStyling.TertiaryColor.ToHex();
                }

                button.OnClick += async (sender, e) =>
                {
                    _sort = sortValue;
                    this.UpdateSort(sortValue);
                    await this.Reload();
                };

                _sortButtons.Children.Add(button);
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
            _selectionGroup.Unselect();

            await webElement.Clear();

            await webElement.AddChild(_sortButtons);

            await this.TryLoad();
        }

        private void UpdateSort(Enum sort)
        {
            foreach (ButtonComponent button in _sortButtons.Children.OfType<ButtonComponent>())
            {
                if (button.InnerText == sort.ToString())
                {
                    button.BackgroundColor = _applicationStyling.TertiaryColor.ToHex();
                }
                else
                {
                    button.BackgroundColor = _applicationStyling.SecondaryColor.ToHex();
                }
            }
        }

        private async void WebElement_ClickUrl(object? sender, string e)
        {
            if (!_aggregateUrlHandler.CanLaunch(e, _aggregatePostHandler))
            {
                await this.DisplayAlert("Alert", $"Can not handle url {e}", "OK");
                return;
            }

            await _aggregateUrlHandler.Launch(e, _aggregatePostHandler);
        }

        private void WebElement_OnJavascriptError(object? sender, Exception e)
        {
            _displayExceptions.DisplayException(e);
        }
    }
}