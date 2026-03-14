using Deaddit.Components.WebComponents.Partials.Post;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Blocking;
using Deaddit.Core.Utils.MultiSelect;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Pages;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using Reddit.Api.Models.Json.Subreddits;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("reddit-post")]
    public class RedditPostWebComponent : DivComponent, ISelectionGroupItem
    {
        public readonly ApiPost _post;

        private readonly ActionButtonsComponent _actionButtons;

        private readonly IAggregatePostHandler _apiPostHandler;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly string _backgroundColor;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private readonly IDisplayMessages _displayMessages;

        private readonly string _highlightColor;

        private readonly IHistoryTracker _historyTracker;

        private readonly MultiSelector _multiselector;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup? _selectionGroup;

        private readonly TextContainerComponent _textContainer;

        private readonly IVisitTracker _visitTracker;

        private bool _isFromHistoryPage = false;

        public RedditPostWebComponent(ApiPost post, PostState postHandling, IDisplayMessages displayMessages, ISelectBoxDisplay selectBoxDisplay, IAggregatePostHandler apiPostHandler, ApplicationHacks applicationHacks, BlockConfiguration blockConfiguration, IConfigurationService configurationService, IAppNavigator appNavigator, IVisitTracker visitTracker, IHistoryTracker historyTracker, INavigation navigation, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup? selectionGroup, bool showVisitedState = true)
        {
            _multiselector = new MultiSelector(selectBoxDisplay);
            _post = post;
            _applicationStyling = applicationStyling;
            _redditClient = redditClient;
            _selectionGroup = selectionGroup;
            _visitTracker = visitTracker;
            _historyTracker = historyTracker;
            _navigation = navigation;
            _appNavigator = appNavigator;
            _blockConfiguration = blockConfiguration;
            _configurationService = configurationService;
            _apiPostHandler = apiPostHandler;
            _displayMessages = displayMessages;

            if (postHandling == PostState.Block)
            {
                _backgroundColor = applicationStyling.BlockedBackgroundColor.ToHex();
                _highlightColor = applicationStyling.BlockedBackgroundColor.ToHex();
            }
            else
            {
                _backgroundColor = applicationStyling.SecondaryColor.ToHex();
                _highlightColor = applicationStyling.HighlightColor.ToHex();
            }

            Display = "flex";
            FlexDirection = "column";
            BackgroundColor = _backgroundColor;

            _actionButtons = new ActionButtonsComponent(applicationStyling, _post);
            _actionButtons.SaveClicked += this.SaveButton_OnClick;
            _actionButtons.ShareClicked += this.ShareButton_OnClick;
            _actionButtons.MoreClicked += this.OnMoreClicked;
            _actionButtons.HideClicked += this.HideButton_OnClick;
            _actionButtons.CommentsClicked += this.CommentsButton_OnClick;

            string? thumbSrc = post.TryGetThumbnail();

            if (string.IsNullOrWhiteSpace(thumbSrc) && !string.IsNullOrWhiteSpace(post.Url))
            {
                string mimeType = UrlHelper.GetMimeTypeFromUri(new Uri(post.Url));

                if (mimeType.StartsWith("image/"))
                {
                    thumbSrc = post.Url;
                }
            }

            ImgComponent thumbnail = new()
            {
                Src = thumbSrc,
                Width = $"{applicationStyling.ThumbnailSize}px",
                Height = $"{applicationStyling.ThumbnailSize}px",
                FlexShrink = "0",
                ObjectFit = "cover"
            };

            _textContainer = new(post, applicationStyling, applicationHacks);

            if (selectionGroup is null)
            {
                _textContainer.ShowTimeUser(true);
            }

            VoteContainerComponent voteContainer = new(applicationStyling, post, _redditClient);

            _textContainer.OnClick += this.TextContainer_OnClick;
            thumbnail.OnClick += this.Thumbnail_OnClick;

            DivComponent topContainer = new()
            {
                Display = "flex",
                FlexDirection = "row",
                Width = "100%",
            };

            topContainer.Children.Add(thumbnail);
            topContainer.Children.Add(_textContainer);
            topContainer.Children.Add(voteContainer);

            Children.Add(topContainer);
            Children.Add(_actionButtons);

            if (showVisitedState && selectionGroup != null)
            {
                if (visitTracker.HasVisited(_post) || postHandling == PostState.Visited)
                {
                    Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
                }
            }
        }

        public event EventHandler<BlockRule> BlockAdded;

        public event EventHandler<OnHideClickedEventArgs> HideClicked;

        public bool IsListView => _selectionGroup != null;

        public bool SelectEnabled => true;

        public Task Select()
        {
            BackgroundColor = _highlightColor;
            _actionButtons.BackgroundColor = _highlightColor;
            _textContainer.ShowTimeUser(true);
            _actionButtons.Display = "flex";
            return Task.CompletedTask;
        }

        public void SetHistorySource()
        {
            _isFromHistoryPage = true;
        }

        public Task Unselect()
        {
            BackgroundColor = _backgroundColor;
            _actionButtons.BackgroundColor = _backgroundColor;
            _textContainer.ShowTimeUser(false);
            _actionButtons.Display = "none";
            return Task.CompletedTask;
        }

        private async void CommentsButton_OnClick(object? sender, EventArgs e)
        {
            if (_post.IsSelf)
            {
                Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
                _visitTracker.Visit(_post);
            }

            await _appNavigator.OpenPost(_post, null, _isFromHistoryPage);
        }

        private async void HideButton_OnClick(object? sender, EventArgs e)
        {
            if (_redditClient.CanLogIn)
            {
                await _redditClient.ToggleVisibility(_post, false);
            }

            HideClicked?.Invoke(this, new OnHideClickedEventArgs(_post, this));
        }

        private async Task NewBlockRule(BlockRule blockRule, bool whitelist = false)
        {
            WebObjectEditorPage objectEditorPage = await _appNavigator.OpenObjectEditor(blockRule);

            objectEditorPage.OnSave += (sender, e) =>
            {
                if (e.Saved is BlockRule blockRule)
                {
                    if (whitelist)
                    {
                        _blockConfiguration.WhiteList.Rules.Add(blockRule);
                    }
                    else
                    {
                        _blockConfiguration.BlackList.Rules.Add(blockRule);
                        BlockAdded?.Invoke(this, blockRule);
                    }

                    _configurationService.Write(_blockConfiguration);
                }
            };
        }

        private async Task OnMoreBlockClicked()
        {
            await _multiselector.Select(
            "Block:",
            new($"/u/{_post.Author}", async () => await this.NewBlockRule(BlockRuleHelper.FromAuthor(_post))),
            new($"/r/{_post.SubRedditName}", async () => await this.NewBlockRule(BlockRuleHelper.FromSubReddit(_post))),
            new(_post.Domain, async () => await this.NewBlockRule(BlockRuleHelper.FromDomain(_post))),
            new(_post.LinkFlairText, async () => await this.NewBlockRule(BlockRuleHelper.FromFlair(_post))));
        }

        private async void OnMoreClicked(object? sender, EventArgs e)
        {
            await _multiselector.Select(
            "Select:",
            new($"View...", this.OnMoreViewClicked),
            new($"Share...", this.OnMoreShareClicked),
            new($"Block...", this.OnMoreBlockClicked),
            new($"Whitelist...", this.OnMoreWhitelistClicked),
            new($"Report...", this.OnReportClicked));
        }

        private async Task OnMoreShareClicked()
        {
            await _multiselector.Select(
                "Share:",
                new(null, null),
                new($"Comments", async () => { }));
        }

        private async Task OnMoreViewClicked()
        {
            await _multiselector.Select(
            "View:",
            new($"/u/{_post.Author}", async () => await _appNavigator.OpenUser(_post.Author)),
            new($"/r/{_post.SubRedditName}", async () => await _appNavigator.OpenSubReddit(_post.SubRedditName, ApiPostSort.Hot)));
        }

        private async Task OnMoreWhitelistClicked()
        {
            await _multiselector.Select(
            "Whitelist:",
            new($"/u/{_post.Author}", async () => await this.NewBlockRule(BlockRuleHelper.FromAuthor(_post), true)),
            new($"/r/{_post.SubRedditName}", async () => await this.NewBlockRule(BlockRuleHelper.FromSubReddit(_post), true)),
            new(_post.Domain, async () => await this.NewBlockRule(BlockRuleHelper.FromDomain(_post), true)),
            new(_post.LinkFlairText, async () => await this.NewBlockRule(BlockRuleHelper.FromFlair(_post), true)));
        }

        private async Task OnReportClicked()
        {
            await _multiselector.Select(
                "Report:",
                new("Breaks Reddit Rules", this.OnReportRedditRulesClicked),
                new("Breaks Subreddit Rules", this.OnReportSubredditRulesClicked));
        }

        private async Task OnReportRedditRulesClicked()
        {
            try
            {
                SubredditRulesResponse? rules = await _redditClient.GetSubredditRules(_post.SubRedditName);

                if (rules?.SiteRules == null || rules.SiteRules.Count == 0)
                {
                    await _navigation.NavigationStack[^1].DisplayAlert("Report", "No Reddit rules available", "OK");
                    return;
                }

                MultiSelectOption[] items = rules.SiteRules.Select(rule =>
                    new MultiSelectOption(rule, async () =>
                    {
                        await _redditClient.Report(_post, siteReason: rule);
                        await _navigation.NavigationStack[^1].DisplayAlert("Report", "Post reported successfully", "OK");
                    })).ToArray();

                await _multiselector.Select("Select Rule:", items);
            }
            catch (Exception ex)
            {
                await _displayMessages.DisplayException(ex);
            }
        }

        private async Task OnReportSubredditRulesClicked()
        {
            try
            {
                SubredditRulesResponse? rules = await _redditClient.GetSubredditRules(_post.SubRedditName);

                if (rules?.Rules == null || rules.Rules.Count == 0)
                {
                    await _navigation.NavigationStack[^1].DisplayAlert("Report", $"r/{_post.SubRedditName} has no specific rules", "OK");
                    return;
                }

                MultiSelectOption[] items = rules.Rules.Select(rule =>
                    new MultiSelectOption(rule.ShortName, async () =>
                    {
                        string ruleReason = rule.ViolationReason ?? rule.ShortName;
                        await _redditClient.Report(_post, ruleReason: ruleReason);
                        await _navigation.NavigationStack[^1].DisplayAlert("Report", "Post reported successfully", "OK");
                    })).ToArray();

                await _multiselector.Select("Select Rule:", items);
            }
            catch (Exception ex)
            {
                await _displayMessages.DisplayException(ex);
            }
        }

        private async Task OnShareFileClicked()
        {
            // Try post handler first (galleries, Reddit videos)
            if (_apiPostHandler.CanShare(_post))
            {
                await _apiPostHandler.Share(_post);
                return;
            }

            // Fall back to URL handler (images, videos by URL)
            if (_apiPostHandler.UrlHandler.CanDownload(_post.Url, _apiPostHandler))
            {
                FileDownload download = await _apiPostHandler.UrlHandler.Download(_post.Url, _apiPostHandler);
                await Share.Default.ShareFiles(_post.Title, download);
            }
        }

        private async Task OnShareLinkClicked()
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = _post.Url,
                Title = _post.Title
            });
        }

        private async void SaveButton_OnClick(object? sender, EventArgs e)
        {
            if (_post.Saved == true)
            {
                await _redditClient.ToggleSave(_post, false);
                _post.Saved = false;
                _actionButtons.UpdateSaveButtonText();
            }
            else
            {
                await _redditClient.ToggleSave(_post, true);
                _post.Saved = true;
                _actionButtons.UpdateSaveButtonText();
            }
        }

        private async void ShareButton_OnClick(object? sender, EventArgs e)
        {
            if (_apiPostHandler.CanDownload(_post))
            {
                await _multiselector.Select(
                    "Share:",
                    new("File", this.OnShareFileClicked),
                    new("Link", this.OnShareLinkClicked));
            }
            else
            {
                await this.OnShareLinkClicked();
            }
        }

        private async void TextContainer_OnClick(object? sender, EventArgs e)
        {
            if (_selectionGroup != null)
            {
                await _selectionGroup.Toggle(this);
            }
        }

        private async void Thumbnail_OnClick(object? sender, EventArgs e)
        {
            if (_post.IsSelf && !IsListView)
            {
                return;
            }

            if (!_apiPostHandler.CanLaunch(_post))
            {
                await _navigation.NavigationStack[^1].DisplayAlert("Alert", $"Can not handle post", "OK");
                return;
            }

            if (_selectionGroup != null)
            {
                Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
                await _selectionGroup.Select(this);
                _visitTracker.Visit(_post);
            }

            _historyTracker.AddToHistory(_post, _isFromHistoryPage);

            try
            {
                await _apiPostHandler.Launch(_post);
            }
            catch (Exception ex)
            {
                await _displayMessages.DisplayException(ex);
            }
        }
    }
}