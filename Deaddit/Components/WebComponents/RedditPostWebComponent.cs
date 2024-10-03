using Deaddit.Components.WebComponents.Partials.Post;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.Options;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Extensions;
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

        private const string TXT_COMMENT = "🗨";

        private const string TXT_DOTS = "...";

        private const string TXT_HIDE = "Hide";

        private const string TXT_SAVE = "Save";

        private const string TXT_SHARE = "Share";

        private const string TXT_UNSAVE = "Unsave";

        private readonly ActionButtonsComponent _actionButtons;

        private readonly IAggregatePostHandler _apiPostHandler;

        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly string _backgroundColor;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private readonly string _highlightColor;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup? _selectionGroup;

        private readonly TextContainerComponent _textContainer;

        private readonly IVisitTracker _visitTracker;

        private bool _isDisposed = false;

        public bool SelectEnabled => true;

        public event EventHandler<BlockRule> BlockAdded;

        public event EventHandler<OnHideClickedEventArgs> HideClicked;

        public RedditPostWebComponent(ApiPost post, bool blocked, IAggregatePostHandler apiPostHandler, ApplicationHacks applicationHacks, BlockConfiguration blockConfiguration, IConfigurationService configurationService, IAppNavigator appNavigator, IVisitTracker visitTracker, INavigation navigation, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup? selectionGroup)
        {
            _post = post;
            _applicationStyling = applicationStyling;
            _redditClient = redditClient;
            _selectionGroup = selectionGroup;
            _visitTracker = visitTracker;
            _navigation = navigation;
            _appNavigator = appNavigator;
            _blockConfiguration = blockConfiguration;
            _configurationService = configurationService;
            _applicationHacks = applicationHacks;
            _apiPostHandler = apiPostHandler;

            if (blocked)
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
            _actionButtons.MoreClicked += this.MoreButton_OnClick;
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

            if (visitTracker.HasVisited(_post) && selectionGroup != null)
            {
                Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
        }

        public void Select()
        {
            BackgroundColor = _highlightColor;
            _actionButtons.BackgroundColor = _highlightColor;
            _textContainer.ShowTimeUser(true);
            _actionButtons.Display = "flex";
        }

        public void Unselect()
        {
            BackgroundColor = _backgroundColor;
            _actionButtons.BackgroundColor = _backgroundColor;
            _textContainer.ShowTimeUser(false);
            _actionButtons.Display = "none";
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

        private async void MoreButton_OnClick(object? sender, EventArgs e)
        {
            Dictionary<PostMoreOptions, string?> options = [];

            options.Add(PostMoreOptions.BlockAuthor, $"Block /u/{_post.Author}");
            options.Add(PostMoreOptions.BlockSubreddit, $"Block /r/{_post.SubReddit}");
            options.Add(PostMoreOptions.ViewAuthor, $"View /u/{_post.Author}");
            options.Add(PostMoreOptions.ViewSubreddit, $"View /r/{_post.SubReddit}");

            if (!string.IsNullOrWhiteSpace(_post.Domain))
            {
                options.Add(PostMoreOptions.BlockDomain, $"Block {_post.Domain}");
            }
            else
            {
                options.Add(PostMoreOptions.BlockDomain, null);
            }

            if (!string.IsNullOrWhiteSpace(_post.LinkFlairText))
            {
                options.Add(PostMoreOptions.BlockFlair, $"Block [{_post.LinkFlairText}]");
            }
            else
            {
                options.Add(PostMoreOptions.BlockFlair, null);
            }

            PostMoreOptions? postMoreOptions = await _navigation.NavigationStack[^1].DisplayActionSheet("Select:", null, null, options);

            if (postMoreOptions is null)
            {
                return;
            }

            switch (postMoreOptions.Value)
            {
                case PostMoreOptions.ViewAuthor:
                    await _appNavigator.OpenUser(_post.Author);
                    break;

                case PostMoreOptions.BlockFlair:
                    await this.NewBlockRule(new BlockRule()
                    {
                        Flair = _post.LinkFlairText,
                        SubReddit = _post.SubReddit,
                        BlockType = BlockType.Post,
                        RuleName = $"{_post.SubReddit} [{_post.LinkFlairText}]"
                    });
                    break;

                case PostMoreOptions.BlockSubreddit:
                    await this.NewBlockRule(new BlockRule()
                    {
                        SubReddit = _post.SubReddit,
                        BlockType = BlockType.Post,
                        RuleName = $"/r/{_post.SubReddit}"
                    });
                    break;

                case PostMoreOptions.ViewSubreddit:
                    await _appNavigator.OpenSubReddit(_post.SubReddit, ApiPostSort.Hot);
                    break;

                case PostMoreOptions.BlockAuthor:
                    await this.NewBlockRule(new BlockRule()
                    {
                        Author = _post.Author,
                        BlockType = BlockType.Post,
                        RuleName = $"/u/{_post.Author}"
                    });
                    break;

                case PostMoreOptions.BlockDomain:
                    if (!string.IsNullOrWhiteSpace(_post.Domain))
                    {
                        await this.NewBlockRule(new BlockRule()
                        {
                            Domain = _post.Domain,
                            BlockType = BlockType.Post,
                            RuleName = $"({_post.Domain})"
                        });
                    }

                    break;

                default: throw new EnumNotImplementedException(postMoreOptions.Value);
            }
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

        private void TextContainer_OnClick(object? sender, EventArgs e)
        {
            _selectionGroup?.Toggle(this);
        }

        private async void Thumbnail_OnClick(object? sender, EventArgs e)
        {
            if (!_apiPostHandler.CanLaunch(_post))
            {
                await _navigation.NavigationStack[^1].DisplayAlert("Alert", $"Can not handle post", "OK");
                return;
            }

            await _apiPostHandler.Launch(_post);
        }
    }
}