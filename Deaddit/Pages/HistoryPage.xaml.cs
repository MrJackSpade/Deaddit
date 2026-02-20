using Deaddit.Components.WebComponents;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Validation;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Utils;
using Maui.WebComponents.Extensions;
using Reddit.Api.Interfaces;
using Reddit.Api.Models.Api;
using System.Diagnostics;

namespace Deaddit.Pages
{
    public partial class HistoryPage : ContentPage
    {
        private const int PageSize = 25;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly IDisplayMessages _displayExceptions;

        private readonly IHistoryTracker _historyTracker;

        private readonly List<RedditPostWebComponent> _loadedComponents = [];

        private readonly SemaphoreSlim _loadSemaphore = new(1);

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private int _currentIndex = 0;

        private IReadOnlyList<string> _historyIds = [];

        public HistoryPage(IHistoryTracker historyTracker, IDisplayMessages displayExceptions, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _historyTracker = Ensure.NotNull(historyTracker);
            _appNavigator = Ensure.NotNull(appNavigator);
            _redditClient = Ensure.NotNull(redditClient);
            _applicationStyling = Ensure.NotNull(applicationStyling);
            _displayExceptions = Ensure.NotNull(displayExceptions);

            _selectionGroup = new SelectionGroup();

            this.InitializeComponent();

            webElement.SetColors(applicationStyling);
            webElement.OnJavascriptError += this.WebElement_OnJavascriptError;
            webElement.OnScrollBottom += this.ScrollDown;

            navigationBar.BackgroundColor = applicationStyling.PrimaryColor.ToMauiColor();
            titleLabel.TextColor = applicationStyling.TextColor.ToMauiColor();
            settingsButton.TextColor = applicationStyling.TextColor.ToMauiColor();
            reloadButton.TextColor = applicationStyling.TextColor.ToMauiColor();
        }

        public async Task Init()
        {
            _historyIds = _historyTracker.GetHistory();
            _currentIndex = 0;
            await this.TryLoad();
        }

        public async void OnReloadClicked(object? sender, EventArgs e)
        {
            if (_loadSemaphore.Wait(0))
            {
                await this.Reload();
                _loadSemaphore.Release();
            }
        }

        public async void OnSettingsClicked(object? sender, EventArgs e)
        {
            await _appNavigator.OpenObjectEditor();
        }

        public async void ScrollDown(object? sender, EventArgs e)
        {
            if (_loadSemaphore.Wait(0))
            {
                await this.TryLoad();
                _loadSemaphore.Release();
            }
        }

        private async void RedditPostComponent_HideClicked(object? sender, OnHideClickedEventArgs e)
        {
            await webElement.RemoveChild(e.Component);
        }

        private async Task Reload()
        {
            _loadedComponents.Clear();
            _historyIds = _historyTracker.GetHistory();
            _currentIndex = 0;
            await _selectionGroup.Unselect();
            await webElement.Clear();
            await this.TryLoad();
        }

        private async Task TryLoad()
        {
            await DataService.LoadAsync(webElement, async () =>
            {
                int remaining = Math.Min(PageSize, _historyIds.Count - _currentIndex);

                if (remaining <= 0)
                {
                    return;
                }

                List<string> idsToLoad = _historyIds.Skip(_currentIndex).Take(remaining).ToList();
                _currentIndex += idsToLoad.Count;

                // Fetch all posts in a single API call
                List<ApiPost> posts = await _redditClient.GetPosts(idsToLoad);

                // Create a dictionary to maintain order
                Dictionary<string, ApiPost> postsByName = posts.ToDictionary(p => p.Name, p => p);

                // Add components in the original order
                foreach (string postId in idsToLoad)
                {
                    string fullName = postId.StartsWith("t3_") ? postId : $"t3_{postId}";

                    if (!postsByName.TryGetValue(fullName, out ApiPost post) &&
                        !postsByName.TryGetValue(postId, out post))
                    {
                        continue;
                    }

                    RedditPostWebComponent redditPostComponent = _appNavigator.CreatePostWebComponent(post, PostState.None, _selectionGroup);
                    redditPostComponent.SetHistorySource();

                    redditPostComponent.HideClicked += this.RedditPostComponent_HideClicked;

                    _loadedComponents.Add(redditPostComponent);
                    await webElement.AddChild(redditPostComponent);
                }
            }, _applicationStyling.HighlightColor.ToHex());
        }

        private void WebElement_OnJavascriptError(object? sender, Exception e)
        {
            _displayExceptions.DisplayException(e);
        }
    }
}
