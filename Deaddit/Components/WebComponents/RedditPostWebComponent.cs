using Deaddit.Components.WebComponents.Partials.Post;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Blocking;
using Deaddit.Core.Utils.MultiSelect;
using Deaddit.EventArguments;
using Deaddit.Interfaces;
using Deaddit.Pages;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;

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

        private readonly string _highlightColor;

        private readonly MultiSelector _multiselector;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup? _selectionGroup;

        private readonly TextContainerComponent _textContainer;

        private readonly IVisitTracker _visitTracker;

        public bool SelectEnabled => true;

        public event EventHandler<BlockRule> BlockAdded;

        public event EventHandler<OnHideClickedEventArgs> HideClicked;

        public RedditPostWebComponent(ApiPost post, PostState postHandling, ISelectBoxDisplay selectBoxDisplay, IAggregatePostHandler apiPostHandler, ApplicationHacks applicationHacks, BlockConfiguration blockConfiguration, IConfigurationService configurationService, IAppNavigator appNavigator, IVisitTracker visitTracker, INavigation navigation, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup? selectionGroup)
        {
            _multiselector = new MultiSelector(selectBoxDisplay);
            _post = post;
            _applicationStyling = applicationStyling;
            _redditClient = redditClient;
            _selectionGroup = selectionGroup;
            _visitTracker = visitTracker;
            _navigation = navigation;
            _appNavigator = appNavigator;
            _blockConfiguration = blockConfiguration;
            _configurationService = configurationService;
            _apiPostHandler = apiPostHandler;

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

            DivComponent topContainer = new()
            {
                Display = "flex",
                FlexDirection = "row",
                Width = "100%",
            };

            _actionButtons = new ActionButtonsComponent(applicationStyling, _post);
            _actionButtons.SaveClicked += this.SaveButton_OnClick;
            _actionButtons.ShareClicked += this.ShareButton_OnClick;
            _actionButtons.MoreClicked += this.OnMoreClicked;
            _actionButtons.HideClicked += this.HideButton_OnClick;
            _actionButtons.CommentsClicked += this.CommentsButton_OnClick;

            ImgComponent thumbnail = new()
            {
                Src = post.TryGetPreview(),
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

            topContainer.Children.Add(thumbnail);
            topContainer.Children.Add(_textContainer);
            topContainer.Children.Add(voteContainer);

            Children.Add(topContainer);
            Children.Add(_actionButtons);

            if (selectionGroup != null)
            {
                if (visitTracker.HasVisited(_post) || postHandling == PostState.Visited)
                {
                    Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
                }
            }
        }

        public Task Select()
        {
            BackgroundColor = _highlightColor;
            _actionButtons.BackgroundColor = _highlightColor;
            _textContainer.ShowTimeUser(true);
            _actionButtons.Display = "flex";
            return Task.CompletedTask;
        }

        public Task Unselect()
        {
            BackgroundColor = _backgroundColor;
            _actionButtons.BackgroundColor = _backgroundColor;
            _textContainer.ShowTimeUser(false);
            _actionButtons.Display = "none";
            return Task.CompletedTask;
        }

        private ButtonComponent ActionButton(string text)
        {
            return new ButtonComponent
            {
                InnerText = text,
                FontSize = $"{_applicationStyling.TitleFontSize}px",
                Color = _applicationStyling.TextColor.ToHex(),
                BackgroundColor = _applicationStyling.HighlightColor.ToHex(),
                Padding = "10px",
                FlexGrow = "1",
                Border = "0",
            };
        }

        private void BlockRuleOnSave(object? sender, Deaddit.EventArguments.ObjectEditorSaveEventArgs e)
        {
            if (e.Saved is BlockRule blockRule)
            {
                _blockConfiguration.BlockRules.Add(blockRule);

                _configurationService.Write(_blockConfiguration);

                BlockAdded?.Invoke(this, blockRule);
            }
        }

        private async void CommentsButton_OnClick(object? sender, EventArgs e)
        {
            if (_post.IsSelf && _selectionGroup is null)
            {
                Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
                _visitTracker.Visit(_post);
            }

            await _appNavigator.OpenPost(_post);
        }

        private async void HideButton_OnClick(object? sender, EventArgs e)
        {
            await _redditClient.ToggleVisibility(_post, false);
            HideClicked?.Invoke(this, new OnHideClickedEventArgs(_post, this));
        }

        private async Task OnMoreShareClicked()
        {
            await _multiselector.Select(
                "Share:",
                new(null, null),
                new($"Comments", async () => await this.NewBlockRule(BlockRuleHelper.FromAuthor(_post))));
        }

        private async void OnMoreClicked(object? sender, EventArgs e)
        {
            await _multiselector.Select(
            "Select:",
            new($"Block...", this.OnMoreBlockClicked),
            new($"View...", this.OnMoreViewClicked),
            new($"Share...", this.OnMoreShareClicked));
        }

        private async Task OnMoreBlockClicked()
        {
            await _multiselector.Select(
            "Block:",
            new($"/u/{_post.Author}", async () => await this.NewBlockRule(BlockRuleHelper.FromAuthor(_post))),
            new($"/r/{_post.SubReddit}", async () => await this.NewBlockRule(BlockRuleHelper.FromSubReddit(_post))),
            new(_post.Domain, async () => await this.NewBlockRule(BlockRuleHelper.FromDomain(_post))),
            new(_post.LinkFlairText, async () => await this.NewBlockRule(BlockRuleHelper.FromFlair(_post))));

        }

        private async Task OnMoreViewClicked()
        {
            await _multiselector.Select(
            "View:",
            new($"/u/{_post.Author}", async () => await _appNavigator.OpenUser(_post.Author)),
            new($"/r/{_post.SubReddit}", async () => await _appNavigator.OpenSubReddit(_post.SubReddit, ApiPostSort.Hot)));
        }

        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = await _appNavigator.OpenObjectEditor(blockRule);

            objectEditorPage.OnSave += this.BlockRuleOnSave;
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
            if (!_apiPostHandler.CanShare(_post))
            {
                await _navigation.NavigationStack[^1].DisplayAlert("Alert", $"Can not handle post", "OK");
                return;
            }

            await _apiPostHandler.Share(_post);
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

            await _apiPostHandler.Launch(_post);
        }
    }
}