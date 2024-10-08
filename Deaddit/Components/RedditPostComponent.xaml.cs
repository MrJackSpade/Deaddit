﻿using Deaddit.Core.Configurations.Interfaces;
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
using Deaddit.Core.Utils.Extensions;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Pages;
using Microsoft.Maui.Controls.Shapes;

namespace Deaddit.MAUI.Components
{
    public partial class RedditPostComponent : ContentView, ISelectionGroupItem
    {
        private const string TXT_COMMENT = "🗨";

        private const string TXT_DOTS = "...";

        private const string TXT_HIDE = "Hide";

        private const string TXT_SAVE = "Save";

        private const string TXT_SHARE = "Share";

        private const string TXT_UNSAVE = "Unsave";

        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private readonly bool _isBlocked;

        private readonly bool _isListView;

        private readonly ApiPost _post;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly IVisitTracker _visitTracker;

        private Grid _actionButtonsGrid;

        private Stream? _cachedImageStream;

        private Label downvoteButton;

        private Grid mainGrid;

        private VerticalStackLayout mainStack;

        private Label metaDataLabel;

        private Button saveButton;

        private Label scoreLabel;

        private ImageButton thumbnailImage;

        private Label timeUserLabel;

        private Label titleLabel;

        private VerticalStackLayout titleStack;

        private Label upvoteButton;

        private Grid voteStack;

        public RedditPostComponent(ApiPost post, bool isListView, bool blocked, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _applicationStyling = applicationTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _selectionGroup = selectionTracker;
            _applicationHacks = applicationHacks;
            _isBlocked = blocked;
            _visitTracker = visitTracker;
            _post = post;
            _appNavigator = appNavigator;
            _isListView = isListView;

            this.InitializePostComponent(_isListView);
            this.SetImageThumbnail(_post, _applicationStyling);
            this.SetTitleLabel(_post, _applicationHacks, _applicationStyling);
            this.SetLinkFlair(_post, _applicationHacks, _applicationStyling);
            this.SetMetaDataLabel(_post, _applicationStyling);
            this.SetTimeUserLabel(_post, _applicationStyling);
            this.SetVoteStack(_applicationStyling);

            this.Initialize();
        }

        public event EventHandler<BlockRule>? BlockAdded;

        public event EventHandler<OnHideClickedEventArgs>? HideClicked;

        public bool IsInitialized { get; private set; }

        public bool Selected { get; private set; }

        public bool SelectEnabled { get; private set; }

        public bool Deinitialize()
        {
            if (!IsInitialized)
            {
                return false;
            }

            HeightRequest = Height;

            IsInitialized = false;

            Content = null;

            return true;
        }

        public bool Initialize()
        {
            if (IsInitialized)
            {
                return false;
            }

            IsInitialized = true;

            Content = mainStack;

            return true;
        }

        public async void OnCommentsClicked(object? sender, EventArgs e)
        {
            if (_post.IsSelf && _isListView)
            {
                Opacity = _applicationStyling.VisitedOpacity;
                _visitTracker.Visit(_post);
            }

            await _appNavigator.OpenPost(_post);
        }

        public void OnDownvoteClicked(object? sender, EventArgs e)
        {
            if (_post.Likes == UpvoteState.Downvote)
            {
                _post.Likes = UpvoteState.None;
                _post.Score++;
            }
            else if (_post.Likes == UpvoteState.Upvote)
            {
                _post.Likes = UpvoteState.Downvote;
                _post.Score -= 2;
            }
            else
            {
                _post.Likes = UpvoteState.Downvote;
                _post.Score--;
            }

            this.UpdateScoreIndicator();
            _redditClient.SetUpvoteState(_post, _post.Likes);
        }

        public async void OnHideClicked(object? sender, EventArgs e)
        {
            await _redditClient.ToggleVisibility(_post, false);
            HideClicked?.Invoke(this, new OnHideClickedEventArgs(_post, this));
        }

        public async void OnMoreOptionsClicked(object? sender, EventArgs e)
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

            PostMoreOptions? postMoreOptions = await this.DisplayActionSheet("Select:", null, null, options);

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

        public async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (_post.Saved == true)
            {
                await _redditClient.ToggleSave(_post, false);
                _post.Saved = false;
                saveButton.Text = TXT_SAVE;
            }
            else
            {
                await _redditClient.ToggleSave(_post, true);
                _post.Saved = true;
                saveButton.Text = TXT_UNSAVE;
            }
        }

        public async void OnShareClicked(object? sender, EventArgs e)
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

        public async void OnThumbnailImageClicked(object? sender, EventArgs e)
        {
            if (_isListView)
            {
                Opacity = _applicationStyling.VisitedOpacity;
                _selectionGroup.Select(this);
                _visitTracker.Visit(_post);
            }

            await Navigation.OpenPost(_post, _appNavigator);
        }

        public void OnUpvoteClicked(object? sender, EventArgs e)
        {
            if (_post.Likes == UpvoteState.Upvote)
            {
                _post.Likes = UpvoteState.None;
                _post.Score--;
            }
            else if (_post.Likes == UpvoteState.Downvote)
            {
                _post.Likes = UpvoteState.Upvote;
                _post.Score += 2;
            }
            else
            {
                _post.Likes = UpvoteState.Upvote;
                _post.Score++;
            }

            this.UpdateScoreIndicator();
            _redditClient.SetUpvoteState(_post, _post.Likes);
        }

        void ISelectionGroupItem.Select()
        {
            Selected = true;
            BackgroundColor = _applicationStyling.HighlightColor.ToMauiColor();
            mainGrid.BackgroundColor = _applicationStyling.HighlightColor.ToMauiColor();
            timeUserLabel.IsVisible = true;
            HeightRequest = -1;
            this.InitActionButtons();
        }

        void ISelectionGroupItem.Unselect()
        {
            Selected = false;

            if (!_isBlocked)
            {
                BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
                mainGrid.BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
            }
            else
            {
                BackgroundColor = _applicationStyling.BlockedBackgroundColor.ToMauiColor();
                mainGrid.BackgroundColor = _applicationStyling.BlockedBackgroundColor.ToMauiColor();
            }

            timeUserLabel.IsVisible = false;
            this.RemoveActionButtons();
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

        private async Task<Stream> GetImageStream(CancellationToken c)
        {
            if (_cachedImageStream is null)
            {
                string? imageUrl = _post.TryGetPreview();
                if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
                {
                    _cachedImageStream = await ImageHelper.ResizeAndCropImageFromUrlAsync(imageUrl, _applicationStyling.ThumbnailSize);
                }
            }

            if (_cachedImageStream is null)
            {
                return null;
            }

            _cachedImageStream.Seek(0, SeekOrigin.Begin);
            return _cachedImageStream;
        }

        private void InitActionButtons()
        {
            // Initialize the Grid (actionButtonsGrid)
            _actionButtonsGrid = new()
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                BackgroundColor = _applicationStyling.HighlightColor.ToMauiColor()
            };

            // Initialize the Share button
            Button shareButton = new()
            {
                Text = TXT_SHARE,
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationStyling.TextColor.ToMauiColor()
            };
            shareButton.Clicked += this.OnShareClicked;

            // Initialize the Save button
            saveButton = new()
            {
                Text = _post.Saved == true ? TXT_UNSAVE : TXT_SAVE,
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationStyling.TextColor.ToMauiColor()
            };
            saveButton.Clicked += this.OnSaveClicked;

            // Initialize the Hide button
            Button hideButton = new()
            {
                Text = TXT_HIDE,
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationStyling.TextColor.ToMauiColor()
            };
            hideButton.Clicked += this.OnHideClicked;

            // Initialize the More Options button
            Button moreButton = new()
            {
                Text = TXT_DOTS,
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationStyling.TextColor.ToMauiColor()
            };
            moreButton.Clicked += this.OnMoreOptionsClicked;

            // Initialize the Comments button
            Button commentsButton = new()
            {
                Text = TXT_COMMENT,
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationStyling.TextColor.ToMauiColor()
            };
            commentsButton.Clicked += this.OnCommentsClicked;

            // Add buttons to the Grid, specifying the column for each button
            _actionButtonsGrid.Add(shareButton, 0, 0);    // Add to column 0
            _actionButtonsGrid.Add(saveButton, 1, 0);     // Add to column 1
            _actionButtonsGrid.Add(hideButton, 2, 0);     // Add to column 2
            _actionButtonsGrid.Add(moreButton, 3, 0);     // Add to column 3
            _actionButtonsGrid.Add(commentsButton, 4, 0); // Add to column 4

            // Add the Grid to the main stack
            mainStack.Children.Add(_actionButtonsGrid);
        }

        private void InitializePostComponent(bool isPreview)
        {
            SelectEnabled = isPreview;

            mainStack = new()
            {
                VerticalOptions = LayoutOptions.StartAndExpand
            };

            mainGrid = new()
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) }
                ],
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            };

            // Thumbnail Image
            thumbnailImage = new()
            {
                Aspect = Aspect.AspectFill,
                Margin = new Thickness(0),
                VerticalOptions = LayoutOptions.Start
            };

            mainGrid.Children.Add(thumbnailImage);
            Grid.SetColumn(thumbnailImage, 0);

            // Title and MetaData Stack
            titleStack = new()
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Margin = new Thickness(0),
                Padding = new Thickness(8, 3, 3, 3)
            };

            TapGestureRecognizer titleTap = new();
            titleTap.Tapped += this.OnParentTapped;
            titleStack.GestureRecognizers.Add(titleTap);

            titleLabel = new()
            {
                HorizontalOptions = LayoutOptions.StartAndExpand,
                LineBreakMode = LineBreakMode.WordWrap,
                VerticalOptions = LayoutOptions.Center
            };
            titleStack.Children.Add(titleLabel);

            metaDataLabel = new()
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(0, 0, 0, 0)
            };
            titleStack.Children.Add(metaDataLabel);

            timeUserLabel = new()
            {
                IsVisible = false,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Padding = new Thickness(0, 0, 0, 0)
            };
            titleStack.Children.Add(timeUserLabel);

            mainGrid.Children.Add(titleStack);
            Grid.SetColumn(titleStack, 1);

            // Vote Stack
            voteStack = new()
            {
                WidthRequest = 50,
                RowDefinitions =
                [
                    new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) },
                    new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                ],
                VerticalOptions = LayoutOptions.Start
            };

            upvoteButton = new()
            {
                Text = "▲",
                BackgroundColor = Colors.Transparent,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start
            };

            TapGestureRecognizer upvoteTap = new();
            upvoteTap.Tapped += this.OnUpvoteClicked;
            upvoteButton.GestureRecognizers.Add(upvoteTap);
            voteStack.Children.Add(upvoteButton);
            Grid.SetRow(upvoteButton, 0);

            scoreLabel = new()
            {
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };
            voteStack.Children.Add(scoreLabel);
            Grid.SetRow(scoreLabel, 1);

            downvoteButton = new()
            {
                Text = "▼",
                BackgroundColor = Colors.Transparent,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                VerticalTextAlignment = TextAlignment.Start,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start
            };
            TapGestureRecognizer downvoteTap = new();
            downvoteTap.Tapped += this.OnDownvoteClicked;
            downvoteButton.GestureRecognizers.Add(downvoteTap);
            voteStack.Children.Add(downvoteButton);
            Grid.SetRow(downvoteButton, 2);

            mainGrid.Children.Add(voteStack);
            Grid.SetColumn(voteStack, 2);

            mainStack.Children.Add(mainGrid);
            Content = mainStack;

            timeUserLabel.IsVisible = !isPreview;
            Opacity = isPreview && _visitTracker.HasVisited(_post) ? _applicationStyling.VisitedOpacity : 1;
            mainGrid.MinimumHeightRequest = _applicationStyling.ThumbnailSize;

            if (!_isBlocked)
            {
                BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
                mainGrid.BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
            }
            else
            {
                BackgroundColor = _applicationStyling.BlockedBackgroundColor.ToMauiColor();
                mainGrid.BackgroundColor = _applicationStyling.BlockedBackgroundColor.ToMauiColor();
            }
        }

        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = await _appNavigator.OpenObjectEditor(blockRule);

            objectEditorPage.OnSave += this.BlockRuleOnSave;
        }

        private void OnParentTapped(object? sender, TappedEventArgs e)
        {
            _selectionGroup.Toggle(this);
        }

        private void RemoveActionButtons()
        {
            if (_actionButtonsGrid is not null)
            {
                mainStack.Children.Remove(_actionButtonsGrid);
                _actionButtonsGrid = null;
            }
        }

        private void SetImageThumbnail(ApiPost post, ApplicationStyling applicationTheme)
        {
            thumbnailImage.HeightRequest = applicationTheme.ThumbnailSize;
            thumbnailImage.WidthRequest = applicationTheme.ThumbnailSize;

            if (!post.IsSelf || _isListView)
            {
                thumbnailImage.Clicked += this.OnThumbnailImageClicked;
            }

            thumbnailImage.Source = ImageSource.FromStream(this.GetImageStream);
        }

        private void SetLinkFlair(ApiPost post, ApplicationHacks applicationHacks, ApplicationStyling applicationTheme)
        {
            string cleanedLinkFlair = applicationHacks.CleanFlair(post.LinkFlairText);

            if (!string.IsNullOrWhiteSpace(cleanedLinkFlair))
            {
                Label linkFlairLabel = new()
                {
                    HorizontalOptions = LayoutOptions.Fill,
                    Margin = new Thickness(2),
                    Text = cleanedLinkFlair,
                    BackgroundColor = applicationTheme.PrimaryColor.ToMauiColor(),
                    FontSize = applicationTheme.FontSize * 0.75,
                    TextColor = post.LinkFlairBackgroundColor.ToMauiColor() ?? applicationTheme.TextColor.ToMauiColor()
                };

                Border linkFlairBorder = new()
                {
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3) },
                    Margin = new Thickness(0, 0, 10, 0),
                    HorizontalOptions = LayoutOptions.Start,
                    BackgroundColor = applicationTheme.PrimaryColor.ToMauiColor(),
                    IsVisible = !string.IsNullOrWhiteSpace(post.LinkFlairText),
                    Content = linkFlairLabel
                };

                if (post.LinkFlairBackgroundColor is not null)
                {
                    linkFlairBorder.Stroke = post.LinkFlairBackgroundColor.ToMauiColor();
                }

                titleStack.InsertAfter(titleLabel, linkFlairBorder);
            }
        }

        private void SetMetaDataLabel(ApiPost post, ApplicationStyling applicationTheme)
        {
            metaDataLabel.Text = $"{post.NumComments} comments {post.SubReddit}";
            metaDataLabel.FontSize = applicationTheme.FontSize * 0.75;
            metaDataLabel.TextColor = applicationTheme.SubTextColor.ToMauiColor();

            if (!post.IsSelf && Uri.TryCreate(post.Url, UriKind.Absolute, out Uri result))
            {
                metaDataLabel.Text += $" ({result.Host})";
            }
        }

        private void SetTimeUserLabel(ApiPost post, ApplicationStyling applicationTheme)
        {
            timeUserLabel.Text = $"{post.CreatedUtc.Elapsed()} by {post.Author}";
            timeUserLabel.FontSize = applicationTheme.FontSize * 0.75;
            timeUserLabel.TextColor = applicationTheme.SubTextColor.ToMauiColor();
        }

        private void SetTitleLabel(ApiPost post, ApplicationHacks applicationHacks, ApplicationStyling applicationTheme)
        {
            titleLabel.Text = applicationHacks.CleanTitle(post.Title);
            titleLabel.TextColor = post.Distinguished == DistinguishedKind.Moderator ? applicationTheme.DistinguishedTitleColor.ToMauiColor() :
                                                                                       applicationTheme.TextColor.ToMauiColor();
        }

        private void SetVoteStack(ApplicationStyling applicationTheme)
        {
            voteStack.HeightRequest = applicationTheme.ThumbnailSize;
            this.UpdateScoreIndicator();
        }

        private void UpdateScoreIndicator()
        {
            scoreLabel.Text = _post.Score.ToString();
            scoreLabel.TextColor = _post.Likes switch
            {
                UpvoteState.Upvote => _applicationStyling.UpvoteColor.ToMauiColor(),
                UpvoteState.Downvote => _applicationStyling.DownvoteColor.ToMauiColor(),
                _ => _applicationStyling.TextColor.ToMauiColor()
            };
            downvoteButton.TextColor = _post.Likes == UpvoteState.Downvote ? _applicationStyling.DownvoteColor.ToMauiColor() : _applicationStyling.TextColor.ToMauiColor();
            upvoteButton.TextColor = _post.Likes == UpvoteState.Upvote ? _applicationStyling.UpvoteColor.ToMauiColor() : _applicationStyling.TextColor.ToMauiColor();
        }
    }
}