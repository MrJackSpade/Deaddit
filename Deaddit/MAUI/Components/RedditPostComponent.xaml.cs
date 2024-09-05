using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Exceptions;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components.ComponentModels;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Extensions;
using Deaddit.MAUI.Pages;
using Deaddit.Reddit.Extensions;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Reddit.Models.Options;
using Deaddit.Utils;

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

        private readonly RedditPostComponentViewModel _postViewModel;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly IVisitTracker _visitTracker;

        private HorizontalStackLayout _actionButtonsStack;

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
            _configurationService = configurationService;

            SelectEnabled = isPreview;

            BindingContext = _postViewModel = new RedditPostComponentViewModel(post, applicationTheme);
            this.InitializeComponent();
            timeUserLabel.IsVisible = !isPreview;

            Opacity = isPreview && visitTracker.HasVisited(post) ? applicationTheme.VisitedOpacity : 1;

            mainGrid.MinimumHeightRequest = applicationTheme.ThumbnailSize;
            mainGrid.BackgroundColor = applicationTheme.SecondaryColor;

            thumbnailImage.HeightRequest = applicationTheme.ThumbnailSize;
            thumbnailImage.WidthRequest = applicationTheme.ThumbnailSize;
            thumbnailImage.Source = post.TryGetPreview();

            titleLabel.Text = applicationHacks.CleanTitle(post.Title);
            titleLabel.TextColor = applicationTheme.TextColor;

            if (_post.LinkFlairBackgroundColor is not null)
            {
                linkFlairBorder.Stroke = _post.LinkFlairBackgroundColor;
            }

            linkFlairBorder.BackgroundColor = _applicationTheme.PrimaryColor;
            linkFlairBorder.IsVisible = !string.IsNullOrWhiteSpace(_post.LinkFlairText);

            linkFlairLabel.Text = applicationHacks.CleanFlair(_post.LinkFlairText);
            linkFlairLabel.BackgroundColor = _applicationTheme.PrimaryColor;
            linkFlairLabel.FontSize = _applicationTheme.FontSize * 0.75;

            linkFlairLabel.TextColor = _post.LinkFlairBackgroundColor ?? _applicationTheme.TextColor;

            metaDataLabel.Text = $"{_post.NumComments} comments {_post.SubReddit}";
            metaDataLabel.FontSize = _applicationTheme.FontSize * 0.75;
            metaDataLabel.TextColor = _applicationTheme.SubTextColor;

            if (!_post.IsSelf && Uri.TryCreate(_post.Url, UriKind.Absolute, out Uri result))
            {
                metaDataLabel.Text += $" ({result.Host})";
            }

            timeUserLabel.Text = $"{_post.CreatedUtc.Elapsed()} by {_post.Author}";
            timeUserLabel.FontSize = _applicationTheme.FontSize * 0.75;
            timeUserLabel.TextColor = _applicationTheme.SubTextColor;
        }

        public event EventHandler<BlockRule>? BlockAdded;

        public event EventHandler<OnHideClickedEventArgs>? HideClicked;

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
                _postViewModel.TryAdjustScore(1);
            }
            else
            {
                _post.Likes = UpvoteState.Downvote;
                _postViewModel.TryAdjustScore(-1);
            }

            _postViewModel.SetUpvoteState(_post.Likes);
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
                _postViewModel.TryAdjustScore(-1);
            }
            else
            {
                _post.Likes = UpvoteState.Upvote;
                _postViewModel.TryAdjustScore(1);
            }

            _postViewModel.SetUpvoteState(_post.Likes);
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
            mainStack.Children.Remove(_actionButtonsStack);
            _actionButtonsStack = null;
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

        private void InitActionButtons()
        {
            // Initialize the Grid (actionButtonsGrid)
            Grid actionButtonsGrid = new()
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
            actionButtonsGrid.Add(shareButton, 0, 0);    // Add to column 0
            actionButtonsGrid.Add(saveButton, 1, 0);     // Add to column 1
            actionButtonsGrid.Add(hideButton, 2, 0);     // Add to column 2
            actionButtonsGrid.Add(moreButton, 3, 0);     // Add to column 3
            actionButtonsGrid.Add(commentsButton, 4, 0); // Add to column 4

            // Add the Grid to the main stack
            mainStack.Children.Add(actionButtonsGrid);
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
    }
}