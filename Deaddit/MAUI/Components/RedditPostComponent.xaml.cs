﻿using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Exceptions;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Extensions;
using Deaddit.MAUI.Pages;
using Deaddit.Reddit.Extensions;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Reddit.Models.Options;
using Deaddit.Utils;
using Deaddit.Utils.Extensions;
using Microsoft.Maui.Controls.Shapes;

namespace Deaddit.MAUI.Components
{
    public partial class RedditPostComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private readonly bool _isPreview;

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

        private Label scoreLabel;

        private ImageButton thumbnailImage;

        private Label timeUserLabel;

        private Label titleLabel;

        private VerticalStackLayout titleStack;

        private Label upvoteButton;

        private Grid voteStack;

        private RedditPostComponent(ApiPost post, bool isPreview, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _applicationTheme = applicationTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _selectionGroup = selectionTracker;
            _applicationHacks = applicationHacks;
            _visitTracker = visitTracker;
            _post = post;
            _isPreview = isPreview;

            this.Initialize();
        }

        public event EventHandler<BlockRule>? BlockAdded;

        public event EventHandler<OnHideClickedEventArgs>? HideClicked;

        public bool IsInitialized { get; private set; }

        public bool Selected { get; private set; }

        public bool SelectEnabled { get; private set; }

        public static RedditPostComponent ListView(ApiPost post, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditPostComponent toReturn = new(post, true, redditClient, applicationTheme, applicationHacks, visitTracker, selectionTracker, blockConfiguration, configurationService);
            return toReturn;
        }

        public static RedditPostComponent PostView(ApiPost post, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditPostComponent toReturn = new(post, false, redditClient, applicationTheme, applicationHacks, visitTracker, selectionTracker, blockConfiguration, configurationService);
            return toReturn;
        }

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

            this.InitializePostComponent(_isPreview);
            this.SetImageThumbnail(_post, _applicationTheme);
            this.SetTitleLabel(_post, _applicationHacks, _applicationTheme);
            this.SetLinkFlair(_post, _applicationHacks, _applicationTheme);
            this.SetMetaDataLabel(_post, _applicationTheme);
            this.SetTimeUserLabel(_post, _isPreview, _applicationTheme);
            this.SetVoteStack(_post, _applicationTheme);

            HeightRequest = -1;

            return true;
        }

        public async void OnCommentsClicked(object? sender, EventArgs e)
        {
            if (_post.IsSelf && _isPreview)
            {
                Opacity = _applicationTheme.VisitedOpacity;
                _visitTracker.Visit(_post);
            }

            PostPage postPage = new(_post, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
            await Navigation.PushAsync(postPage);
            await postPage.TryLoad();
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
            Dictionary<PostMoreOptions, string> options = [];

            Uri.TryCreate(_post.Domain, UriKind.Absolute, out Uri uri);

            options.Add(PostMoreOptions.BlockAuthor, $"Block /u/{_post.Author}");
            options.Add(PostMoreOptions.BlockSubreddit, $"Block /r/{_post.SubReddit}");
            options.Add(PostMoreOptions.ViewAuthor, $"View /u/{_post.Author}");
            options.Add(PostMoreOptions.ViewSubreddit, $"View /r/{_post.SubReddit}");

            if (uri != null)
            {
                options.Add(PostMoreOptions.BlockDomain, $"Block {uri.Host}");
            }

            PostMoreOptions? postMoreOptions = await this.DisplayActionSheet("Select:", null, null, options);

            if (postMoreOptions is null)
            {
                return;
            }

            switch (postMoreOptions.Value)
            {
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
                    SubRedditPage page = new(_post.SubReddit, ApiPostSort.Hot, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
                    await Navigation.PushAsync(page);
                    await page.TryLoad();
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
                    if (uri != null)
                    {
                        await this.NewBlockRule(new BlockRule()
                        {
                            Domain = uri.Host,
                            BlockType = BlockType.Post,
                            RuleName = $"({uri.Host})"
                        });
                    }

                    break;

                default: throw new EnumNotImplementedException(postMoreOptions.Value);
            }
        }

        public void OnSaveClicked(object? sender, EventArgs e)
        {
            // Handle the Save button click
        }

        public async void OnShareClicked(object? sender, EventArgs e)
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = _post.Url,
                Title = _post.Title
            });
        }

        public async void OnThumbnailImageClicked(object? sender, EventArgs e)
        {
            if (_isPreview)
            {
                Opacity = _applicationTheme.VisitedOpacity;
                _visitTracker.Visit(_post);
            }

            await Navigation.OpenPost(_post, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
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
            BackgroundColor = _applicationTheme.HighlightColor;
            mainGrid.BackgroundColor = _applicationTheme.HighlightColor;
            timeUserLabel.IsVisible = true;
            this.InitActionButtons();
        }

        void ISelectionGroupItem.Unselect()
        {
            Selected = false;
            BackgroundColor = _applicationTheme.SecondaryColor;
            mainGrid.BackgroundColor = _applicationTheme.SecondaryColor;
            timeUserLabel.IsVisible = false;
            mainStack.Children.Remove(_actionButtonsGrid);
            _actionButtonsGrid = null;
        }

        private void BlockRuleOnSave(object? sender, MAUI.EventArguments.ObjectEditorSaveEventArgs e)
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
                if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? imageUri))
                {
                    Stream rawStream = await ImageHelper.ResizeAndCropImageFromUrlAsync(imageUrl, _applicationTheme.ThumbnailSize);

                    _cachedImageStream = new PersistentStream(rawStream);
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
                BackgroundColor = _applicationTheme.HighlightColor
            };

            // Initialize the Share button
            Button shareButton = new()
            {
                Text = "Share",
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationTheme.TextColor
            };
            shareButton.Clicked += this.OnShareClicked;

            // Initialize the Save button
            Button saveButton = new()
            {
                Text = "Save",
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationTheme.TextColor
            };
            saveButton.Clicked += this.OnSaveClicked;

            // Initialize the Hide button
            Button hideButton = new()
            {
                Text = "Hide",
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationTheme.TextColor
            };
            hideButton.Clicked += this.OnHideClicked;

            // Initialize the More Options button
            Button moreButton = new()
            {
                Text = "...",
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationTheme.TextColor
            };
            moreButton.Clicked += this.OnMoreOptionsClicked;

            // Initialize the Comments button
            Button commentsButton = new()
            {
                Text = "Comments",
                BackgroundColor = Colors.Transparent,
                TextColor = _applicationTheme.TextColor
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
            thumbnailImage.Clicked += this.OnThumbnailImageClicked;
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
            Opacity = isPreview && _visitTracker.HasVisited(_post) ? _applicationTheme.VisitedOpacity : 1;
            mainGrid.MinimumHeightRequest = _applicationTheme.ThumbnailSize;
            mainGrid.BackgroundColor = _applicationTheme.SecondaryColor;
        }

        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = new(blockRule, _applicationTheme);

            objectEditorPage.OnSave += this.BlockRuleOnSave;

            await Navigation.PushAsync(objectEditorPage);
        }

        private void OnParentTapped(object? sender, TappedEventArgs e)
        {
            _selectionGroup.Toggle(this);
        }

        private void SetImageThumbnail(ApiPost post, ApplicationStyling applicationTheme)
        {
            thumbnailImage.HeightRequest = applicationTheme.ThumbnailSize;
            thumbnailImage.WidthRequest = applicationTheme.ThumbnailSize;

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
                    BackgroundColor = applicationTheme.PrimaryColor,
                    FontSize = applicationTheme.FontSize * 0.75,
                    TextColor = post.LinkFlairBackgroundColor ?? applicationTheme.TextColor
                };

                Border linkFlairBorder = new()
                {
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3) },
                    Margin = new Thickness(0, 0, 10, 0),
                    HorizontalOptions = LayoutOptions.Start,
                    BackgroundColor = applicationTheme.PrimaryColor,
                    IsVisible = !string.IsNullOrWhiteSpace(post.LinkFlairText),
                    Content = linkFlairLabel
                };

                if (post.LinkFlairBackgroundColor is not null)
                {
                    linkFlairBorder.Stroke = post.LinkFlairBackgroundColor;
                }

                titleStack.InsertAfter(titleLabel, linkFlairBorder);
            }
        }

        private void SetMetaDataLabel(ApiPost post, ApplicationStyling applicationTheme)
        {
            metaDataLabel.Text = $"{post.NumComments} comments {post.SubReddit}";
            metaDataLabel.FontSize = applicationTheme.FontSize * 0.75;
            metaDataLabel.TextColor = applicationTheme.SubTextColor;

            if (!post.IsSelf && Uri.TryCreate(post.Url, UriKind.Absolute, out Uri result))
            {
                metaDataLabel.Text += $" ({result.Host})";
            }
        }

        private void SetTimeUserLabel(ApiPost post, bool isPreview, ApplicationStyling applicationTheme)
        {
            timeUserLabel.Text = $"{post.CreatedUtc.Elapsed()} by {post.Author}";
            timeUserLabel.FontSize = applicationTheme.FontSize * 0.75;
            timeUserLabel.TextColor = applicationTheme.SubTextColor;
        }

        private void SetTitleLabel(ApiPost post, ApplicationHacks applicationHacks, ApplicationStyling applicationTheme)
        {
            titleLabel.Text = applicationHacks.CleanTitle(post.Title);
            titleLabel.TextColor = applicationTheme.TextColor;
        }

        private void SetVoteStack(ApiPost post, ApplicationStyling applicationTheme)
        {
            voteStack.HeightRequest = applicationTheme.ThumbnailSize;
            this.UpdateScoreIndicator();
        }

        private void UpdateScoreIndicator()
        {
            scoreLabel.Text = _post.Score.ToString();
            scoreLabel.TextColor = _post.Likes switch
            {
                UpvoteState.Upvote => _applicationTheme.UpvoteColor,
                UpvoteState.Downvote => _applicationTheme.DownvoteColor,
                _ => _applicationTheme.TextColor
            };
            downvoteButton.TextColor = _post.Likes == UpvoteState.Downvote ? _applicationTheme.DownvoteColor : _applicationTheme.TextColor;
            upvoteButton.TextColor = _post.Likes == UpvoteState.Upvote ? _applicationTheme.UpvoteColor : _applicationTheme.TextColor;
        }
    }
}