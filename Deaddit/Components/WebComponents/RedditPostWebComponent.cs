using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Extensions;
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

        private readonly DivComponent _actionButtons;

        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private readonly SpanComponent _downvoteButton;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly ButtonComponent _saveButton;

        private readonly SpanComponent _score;

        private readonly SelectionGroup? _selectionGroup;

        private readonly SpanComponent _timeUser;

        private readonly SpanComponent _upvoteButton;

        private readonly IVisitTracker _visitTracker;

        public bool SelectEnabled => true;

        public event EventHandler<BlockRule> BlockAdded;

        public event EventHandler<OnHideClickedEventArgs> HideClicked;

        public RedditPostWebComponent(ApiPost post, ApplicationHacks applicationHacks, BlockConfiguration blockConfiguration, IConfigurationService configurationService, IAppNavigator appNavigator, IVisitTracker visitTracker, INavigation navigation, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup? selectionGroup)
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

            Display = "flex";
            FlexDirection = "column";
            BackgroundColor = applicationStyling.SecondaryColor.ToHex();

            DivComponent topContainer = new()
            {
                Display = "flex",
                FlexDirection = "row",
                Width = "100%",
            };

            _actionButtons = new()
            {
                Display = "none",
                FlexDirection = "row",
                Width = "100%",
                BackgroundColor = applicationStyling.HighlightColor.ToHex(),
            };

            ButtonComponent shareButton = this.ActionButton(TXT_SHARE);
            _saveButton = this.ActionButton(TXT_SAVE);
            ButtonComponent hideButton = this.ActionButton(TXT_HIDE);
            ButtonComponent moreButton = this.ActionButton(TXT_DOTS);
            ButtonComponent commentsButton = this.ActionButton(TXT_COMMENT);

            _actionButtons.Children.Add(shareButton);
            _actionButtons.Children.Add(_saveButton);
            _actionButtons.Children.Add(hideButton);
            _actionButtons.Children.Add(moreButton);
            _actionButtons.Children.Add(commentsButton);

            ImgComponent thumbnail = new()
            {
                Src = post.TryGetPreview(),
                Width = $"{applicationStyling.ThumbnailSize}px",
                Height = $"{applicationStyling.ThumbnailSize}px",
                ObjectFit = "cover"
            };

            DivComponent textContainer = new()
            {
                Display = "flex",
                FlexDirection = "column",
                Padding = "10px",
                FlexGrow = "1"
            };

            SpanComponent title = new()
            {
                InnerText = post.Title,
                FontSize = $"{applicationStyling.FontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _timeUser = new()
            {
                InnerText = $"{post.CreatedUtc.Elapsed()} by {post.Author}",
                FontSize = $"{(int)(applicationStyling.FontSize * 0.75)}px",
                Color = applicationStyling.SubTextColor.ToHex(),
                Display = "none"
            };

            SpanComponent metaData = new()
            {
                InnerText = $"{post.NumComments} comments {post.SubReddit}",
                FontSize = $"{(int)(applicationStyling.FontSize * 0.75)}px",
                Color = applicationStyling.SubTextColor.ToHex(),
            };

            if (!post.IsSelf && Uri.TryCreate(post.Url, UriKind.Absolute, out Uri result))
            {
                metaData.InnerText += $" ({result.Host})";
            }

            FlairComponent? linkFlair = null;

            string? cleanedLinkFlair = applicationHacks.CleanFlair(post.LinkFlairText);

            if (!string.IsNullOrWhiteSpace(cleanedLinkFlair))
            {
                string color = post.LinkFlairBackgroundColor?.ToHex() ?? applicationStyling.TextColor.ToHex();
                linkFlair = new FlairComponent(cleanedLinkFlair, color, applicationStyling);
                linkFlair.AlignSelf = "flex-start";
            }

            textContainer.Children.Add(title);

            if (linkFlair != null)
            {
                textContainer.Children.Add(linkFlair);
            }

            textContainer.Children.Add(metaData);
            textContainer.Children.Add(_timeUser);

            DivComponent voteContainer = new()
            {
                Display = "flex",
                FlexGrow = "0",
                FlexDirection = "column",
                Padding = "10px"
            };

            _upvoteButton = new()
            {
                TextAlign = "center",
                InnerText = "▲",
                FontSize = $"{applicationStyling.FontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _score = new()
            {
                TextAlign = "center",
                InnerText = post.Score.ToString(),
                FontSize = $"{applicationStyling.FontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _downvoteButton = new()
            {
                TextAlign = "center",
                InnerText = "▼",
                FontSize = $"{applicationStyling.FontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            _downvoteButton.OnClick += this.Downvote;
            _upvoteButton.OnClick += this.Upvote;

            _saveButton.OnClick += this.SaveButton_OnClick;
            commentsButton.OnClick += this.CommentsButton_OnClick;
            moreButton.OnClick += this.MoreButton_OnClick;
            hideButton.OnClick += this.HideButton_OnClick;
            shareButton.OnClick += this.ShareButton_OnClick;

            textContainer.OnClick += this.TextContainer_OnClick;
            thumbnail.OnClick += this.Thumbnail_OnClick;

            voteContainer.Children.Add(_upvoteButton);
            voteContainer.Children.Add(_score);
            voteContainer.Children.Add(_downvoteButton);

            topContainer.Children.Add(thumbnail);
            topContainer.Children.Add(textContainer);
            topContainer.Children.Add(voteContainer);

            Children.Add(topContainer);
            Children.Add(_actionButtons);

            if (visitTracker.HasVisited(_post))
            {
                Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
            }
        }

        public void Select()
        {
            BackgroundColor = _applicationStyling.HighlightColor.ToHex();
            _timeUser.Display = "block";
            _actionButtons.Display = "flex";
        }

        public void Unselect()
        {
            BackgroundColor = _applicationStyling.SecondaryColor.ToHex();
            _timeUser.Display = "none";
            _actionButtons.Display = "none";
        }

        private ButtonComponent ActionButton(string text)
        {
            return new ButtonComponent
            {
                InnerText = text,
                FontSize = $"{_applicationStyling.FontSize}px",
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
            if (_post.IsSelf)
            {
                Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
                _visitTracker.Visit(_post);
            }

            await _appNavigator.OpenPost(_post);
        }

        private void Downvote(object? sender, EventArgs e)
        {
            switch (_post.Likes)
            {
                case UpvoteState.None:
                    _post.Score--;
                    _post.Likes = UpvoteState.Downvote;
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;

                case UpvoteState.Downvote:
                    _post.Score++;
                    _post.Likes = UpvoteState.None;
                    _score.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case UpvoteState.Upvote:
                    _post.Score -= 2;
                    _post.Likes = UpvoteState.Downvote;
                    _score.Color = _applicationStyling.DownvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;
            }

            _score.InnerText = _post.Score?.ToString() ?? string.Empty;
            _redditClient.SetUpvoteState(_post, _post.Likes);
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
                _saveButton.InnerText = TXT_SAVE;
            }
            else
            {
                await _redditClient.ToggleSave(_post, true);
                _post.Saved = true;
                _saveButton.InnerText = TXT_UNSAVE;
            }
        }

        private async void ShareButton_OnClick(object? sender, EventArgs e)
        {
            PostItems target = _post.GetPostItems();

            switch (target.Kind)
            {
                case PostTargetKind.Video:
                case PostTargetKind.Image:
                case PostTargetKind.Audio:
                    await Share.Default.ShareFiles(_post.Title, target);
                    break;

                case PostTargetKind.Undefined:
                case PostTargetKind.Post:
                case PostTargetKind.Url:
                    await Share.Default.RequestAsync(new ShareTextRequest
                    {
                        Uri = _post.Url,
                        Text = _post.Title
                    });
                    break;

                default: throw new EnumNotImplementedException(target.Kind);
            }
        }

        private void TextContainer_OnClick(object? sender, EventArgs e)
        {
            _selectionGroup?.Toggle(this);
        }

        private async void Thumbnail_OnClick(object? sender, EventArgs e)
        {
            if (_selectionGroup != null)
            {
                Opacity = _applicationStyling.VisitedOpacity.ToString("0.00");
                _selectionGroup?.Select(this);
                _visitTracker.Visit(_post);
            }

            await _navigation.OpenPost(_post, _appNavigator);
        }

        private void Upvote(object? sender, EventArgs e)
        {
            switch (_post.Likes)
            {
                case UpvoteState.None:
                    _post.Score++;
                    _post.Likes = UpvoteState.Upvote;
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    break;

                case UpvoteState.Upvote:
                    _post.Score--;
                    _post.Likes = UpvoteState.None;
                    _score.Color = _applicationStyling.TextColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case UpvoteState.Downvote:
                    _post.Score += 2;
                    _post.Likes = UpvoteState.Upvote;
                    _score.Color = _applicationStyling.UpvoteColor.ToHex();
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;
            }

            _score.InnerText = _post.Score.ToString();
            _redditClient.SetUpvoteState(_post, _post.Likes);
        }
    }
}