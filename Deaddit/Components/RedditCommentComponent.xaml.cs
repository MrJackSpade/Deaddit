using Deaddit.Components;
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
using Deaddit.MAUI.Components.Partials;
using Deaddit.Pages;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Deaddit.MAUI.Components
{
    public partial class RedditCommentComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly Dictionary<string, Stream> _cachedImageStreams = [];

        private readonly ApiComment _comment;

        private readonly IRedditClient _redditClient;

        private readonly View commentBody;

        private RedditCommentComponentBottomBar? _bottomBar;

        private VerticalStackLayout? _replies;

        private RedditCommentComponentTopBar? _topBar;

        public IAppNavigator AppNavigator { get; }

        public BlockConfiguration BlockConfiguration { get; }

        public ApiPost Post { get; }

        public bool SelectEnabled { get; private set; }

        public SelectionGroup SelectionGroup { get; }

        public event EventHandler<OnDeleteClickedEventArgs>? OnDelete;

        public RedditCommentComponent(ApiComment comment, ApiPost post, bool selectEnabled, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration)
        {
            SelectEnabled = selectEnabled;
            _applicationStyling = applicationTheme;
            BlockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            Post = post;
            AppNavigator = appNavigator;
            _comment = comment;
            SelectionGroup = selectionTracker;

            this.InitializeComponent();

            if (comment.Distinguished == DistinguishedKind.Moderator)
            {
                authorLabel.TextColor = applicationTheme.ModeratorAuthorTextColor.ToMauiColor();
                authorLabel.BackgroundColor = applicationTheme.ModeratorAuthorBackgroundColor.ToMauiColor();
            }
            else if (comment.Distinguished == DistinguishedKind.Admin)
            {
                authorLabel.TextColor = applicationTheme.AdminAuthorTextColor.ToMauiColor();
                authorLabel.BackgroundColor = applicationTheme.AdminAuthorBackgroundColor.ToMauiColor();
            }
            else if (post is not null && post.Author == comment.Author)
            {
                authorLabel.TextColor = applicationTheme.OpTextColor.ToMauiColor();
                authorLabel.BackgroundColor = applicationTheme.OpBackgroundColor.ToMauiColor();
            }
            else
            {
                authorLabel.TextColor = applicationTheme.TertiaryColor.ToMauiColor();
            }

            authorLabel.Text = comment.Author;

            commentContainer.Background = applicationTheme.SecondaryColor.ToMauiColor();

            if (MarkDownHelper.IsMarkDown(_comment.Body))
            {
                int markdownIndex = commentContainer.Children.IndexOf(contentLabel);
                commentContainer.Children.RemoveAt(markdownIndex);

                // Content Text as Markdown
                MarkdownView markdownView = new()
                {
                    MarkdownText = MarkDownHelper.Clean(_comment.Body),
                    HyperlinkColor = _applicationStyling.HyperlinkColor.ToMauiColor(),
                    TextColor = _applicationStyling.TextColor.ToMauiColor(),
                    H1Color = _applicationStyling.TextColor.ToMauiColor(),
                    H2Color = _applicationStyling.TextColor.ToMauiColor(),
                    H3Color = _applicationStyling.TextColor.ToMauiColor(),
                    TextFontSize = _applicationStyling.FontSize,
                    BlockQuoteBorderColor = _applicationStyling.TextColor.ToMauiColor(),
                    BlockQuoteBackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor(),
                    BlockQuoteTextColor = _applicationStyling.TextColor.ToMauiColor(),
                    Padding = new Thickness(10, 4, 0, 10)
                };

                markdownView.OnHyperLinkClicked += this.OnHyperLinkClicked;

                // Add to the layout
                commentContainer.Children.Insert(markdownIndex, markdownView);
                commentBody = markdownView;
            }
            else
            {
                contentLabel.Text = MarkDownHelper.Clean(_comment.Body);
                contentLabel.TextColor = _applicationStyling.TextColor.ToMauiColor();
                contentLabel.FontSize = _applicationStyling.FontSize;
                contentLabel.Padding = new Thickness(10, 4, 0, 10);
                commentBody = contentLabel;
            }

            metaDataLabel.TextColor = _applicationStyling.SubTextColor.ToMauiColor();
            metaDataLabel.FontSize = _applicationStyling.FontSize * 0.75;

            this.SetIndicatorState(_comment.Likes);

            this.UpdateMetaData();
        }

        [MemberNotNull(nameof(_replies))]
        public void InitChildContainer()
        {
            if (_replies is null)
            {
                _replies = new VerticalStackLayout()
                {
                    VerticalOptions = LayoutOptions.Fill,
                    Margin = new Thickness(15, 0, 0, 0),
                    BackgroundColor = _applicationStyling.TertiaryColor.ToMauiColor(),
                    Padding = new Thickness(1, 0, 0, 0)
                };

                commentContainer.Padding = new Thickness(0, 0, 0, 6);

                commentContainer.Add(_replies);
            }
        }

        public void OnDoneClicked(object? sender, EventArgs e)
        {
            // Handle Done click
        }

        public void OnDownvoteClicked(object? sender, EventArgs e)
        {
            if (_comment.Likes == UpvoteState.Downvote)
            {
                _comment.Score++;
                this.SetIndicatorState(UpvoteState.None);
                _redditClient.SetUpvoteState(_comment, UpvoteState.None);
            }
            else if (_comment.Likes == UpvoteState.Upvote)
            {
                _comment.Score -= 2;
                this.SetIndicatorState(UpvoteState.Downvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Downvote);
            }
            else
            {
                _comment.Score--;
                this.SetIndicatorState(UpvoteState.Downvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Downvote);
            }
        }

        public void OnHideClicked(object? sender, EventArgs e)
        {
            // Handle Hide click
        }

        public async void OnHyperLinkClicked(object? sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);

            PostItems resource = UrlHelper.Resolve(e.Url);

            await Navigation.OpenResource(resource, AppNavigator);
        }

        public void OnParentClicked(object? sender, EventArgs e)
        {
            // Handle Parent click
        }

        public void OnParentTapped(object? sender, TappedEventArgs e)
        {
            SelectionGroup.Toggle(this);
        }

        public async void OnReplyClicked(object? sender, EventArgs e)
        {
            ReplyPage replyPage = await AppNavigator.OpenReplyPage(_comment);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
        }

        public void OnUpvoteClicked(object? sender, EventArgs e)
        {
            if (_comment.Likes == UpvoteState.Upvote)
            {
                _comment.Score--;
                this.SetIndicatorState(UpvoteState.None);
                _redditClient.SetUpvoteState(_comment, UpvoteState.None);
            }
            else if (_comment.Likes == UpvoteState.Downvote)
            {
                _comment.Score += 2;
                this.SetIndicatorState(UpvoteState.Upvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Upvote);
            }
            else
            {
                _comment.Score++;
                this.SetIndicatorState(UpvoteState.Upvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Upvote);
            }
        }

      
        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment, "New comment data");

            e.NewComment.ParentId = _comment.Id;
            e.NewComment.Parent = _comment;

            RedditCommentComponent redditCommentComponent = AppNavigator.CreateCommentComponent(e.NewComment, Post, SelectionGroup);
            redditCommentComponent.OnDelete += (s, e) => _replies.Remove(redditCommentComponent);
            this.InitChildContainer();
            _replies.Children.Insert(0, redditCommentComponent);
        }

        private void SetIndicatorState(UpvoteState state)
        {
            this.UpdateMetaData();

            switch (state)
            {
                case UpvoteState.Upvote:
                    _comment.Likes = UpvoteState.Upvote;
                    voteIndicator.Text = "▲";
                    voteIndicator.TextColor = _applicationStyling.UpvoteColor.ToMauiColor();
                    voteIndicator.IsVisible = true;
                    break;

                case UpvoteState.Downvote:
                    _comment.Likes = UpvoteState.Downvote;
                    voteIndicator.Text = "▼";
                    voteIndicator.TextColor = _applicationStyling.DownvoteColor.ToMauiColor();
                    voteIndicator.IsVisible = true;
                    break;

                default:
                    _comment.Likes = UpvoteState.None;
                    voteIndicator.Text = string.Empty;
                    voteIndicator.TextColor = _applicationStyling.TextColor.ToMauiColor();
                    voteIndicator.IsVisible = false;
                    break;
            }
        }

        private void UpdateMetaData()
        {
            if (!_comment.ScoreHidden == true)
            {
                metaDataLabel.Text = $"{_comment.Score} points {_comment.CreatedUtc.Elapsed()}";
            }
            else
            {
                metaDataLabel.Text = _comment.CreatedUtc.Elapsed();
            }
        }
    }
}