using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Deaddit.Components.WebComponents;
using Deaddit.Components.WebComponents.Partials.User;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.ThingDefinitions;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Extensions;
using Deaddit.Core.Utils.Validation;
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

        private WebComponent? _header = null;

        private readonly ThingDefinition _thingDefinition;

        private string? _after = null;

        private bool? _bufferBack;

        private bool _isBlockEnabled = true;

        private Enum _sort;

        public ThingCollectionPage(ThingDefinition thingDefinition, Enum sort, ApplicationHacks applicationHacks, IDisplayExceptions displayExceptions, IAggregatePostHandler aggregatePostHandler, IAggregateUrlHandler aggregateUrlHandler, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling, BlockConfiguration blockConfiguration)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _sort = sort;
            _applicationHacks = Ensure.NotNull(applicationHacks);
            _appNavigator = Ensure.NotNull(appNavigator);
            _blockConfiguration = Ensure.NotNull(blockConfiguration);
            _thingDefinition = Ensure.NotNull(thingDefinition);
            _redditClient = Ensure.NotNull(redditClient);
            _applicationStyling = Ensure.NotNull(applicationStyling);
            _aggregatePostHandler = Ensure.NotNull(aggregatePostHandler);
            _aggregateUrlHandler = Ensure.NotNull(aggregateUrlHandler);
            _displayExceptions = Ensure.NotNull(displayExceptions);
            _isBlockEnabled = thingDefinition.FilteredByDefault;

            _selectionGroup = new SelectionGroup();
            _sortButtons = new DivComponent()
            {
                Display = "flex",
                FlexDirection = "row",
                Width = "100%",
                BackgroundColor = "red"
            };

            BindingContext = new SubRedditPageViewModel(thingDefinition);

            this.InitializeComponent();

            webElement.SetColors(applicationStyling);
            webElement.ClickUrl += this.WebElement_ClickUrl;
            webElement.OnJavascriptError += this.WebElement_OnJavascriptError;

            if (thingDefinition.Kind == ThingKind.Account)
            {
                _isBlockEnabled = false;
                blockButton.TextColor = applicationStyling.TextColor.ToMauiColor();
            }

            webElement.OnScrollBottom += this.ScrollDown;

            navigationBar.BackgroundColor = applicationStyling.PrimaryColor.ToMauiColor();
            settingsButton.TextColor = applicationStyling.TextColor.ToMauiColor();

            subredditLabel.TextColor = applicationStyling.TextColor.ToMauiColor();
            subredditLabel.Text = thingDefinition.Name;

            reloadButton.TextColor = applicationStyling.TextColor.ToMauiColor();
            infoButton.TextColor = applicationStyling.TextColor.ToMauiColor();

            this.UpdateBlockColor();
        }

        public async void OnBlockClicked(object? sender, EventArgs e)
        {
            _isBlockEnabled = !_isBlockEnabled;

            this.UpdateBlockColor();

            await this.Reload();
        }

        private void UpdateBlockColor()
        {
            if (_isBlockEnabled)
            {
                blockButton.TextColor = Color.Parse("#FF0000");
            }
            else
            {
                blockButton.TextColor = _applicationStyling.TextColor.ToMauiColor();
            }
        }

        public async void OnInfoClicked(object? sender, EventArgs e)
        {
            if (_thingDefinition is SubRedditDefinition sd)
            {
                await _appNavigator.OpenSubRedditAbout(sd.Name);
            }
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

        public async Task Init()
        {
            if (_thingDefinition.Kind == ThingKind.Account)
            {
                ApiUser userData = await _redditClient.GetUserData(_thingDefinition.Name);
                _header = new UserHeader(userData, _applicationStyling);
                await webElement.AddChild(_header);
            }

            if (_sort != null)
            {
                await this.InitSortButtons(_sort);
            }

            await this.TryLoad();
        }

        private async Task TryLoad()
        {
            // Wrap the loading process in a DataService call
            await DataService.LoadAsync(webElement, async () =>
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
                                                                            endpointDefinition: _thingDefinition.EndpointDefinition,
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
                       _thingDefinition.Kind != ThingKind.Account &&
                       //This only works if logged in
                       _redditClient.IsLoggedIn
                       )
                    {
                        userData = await _redditClient.GetPartialUserData(newPosts.Select(p => p.AuthorFullName).Distinct());
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

                //This should be an enum to avoid this weirdness
                //but this ensures the back button override is only enabled
                //if the user has scrolled down to the second page.
                if (_bufferBack is null)
                {
                    _bufferBack = false;
                }
                else
                {
                    _bufferBack = true;
                }
            }, _applicationStyling.HighlightColor.ToHex());
        }

        protected override bool OnBackButtonPressed()
        {
#if ANDROID
            if (_bufferBack != true)
            {
                return false;
            }

            _bufferBack = false;

            string text = "Press back again to exit";
            ToastDuration duration = ToastDuration.Short;
            double fontSize = 14;

            IToast toast = Toast.Make(text, duration, fontSize);

            toast.Show(CancellationToken.None);

            return true;
#else
            return false;
#endif
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
            await _selectionGroup.Unselect();

            await webElement.Clear();

            if (_header is not null)
            {
                await webElement.AddChild(_header);
            }

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