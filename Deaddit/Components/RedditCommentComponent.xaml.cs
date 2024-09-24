using Deaddit.Components;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Extensions;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.Options;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Extensions;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components.Partials;
using Deaddit.Pages;
using Deaddit.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Deaddit.MAUI.Components
{
    public partial class RedditCommentComponent : ContentView, ISelectionGroupItem, IHasChildren
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

        Layout IHasChildren.ChildContainer => _replies;

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

        public async void MoreCommentsClick(object? sender, IMore e)
        {
            MoreCommentsComponent? mcomponent = sender as MoreCommentsComponent;

            if (e.ChildNames.NotNullAny())
            {
                await DataService.LoadAsync(_replies, async () => await this.LoadMoreCommentsAsync(e), _applicationStyling.HighlightColor.ToMauiColor());

                _replies.Remove(mcomponent);
            }
            else
            {
                if (e.Parent is ApiComment parentComment)
                {
                    await this.ContinueThread(parentComment);
                }
                else
                {
                    throw new ArgumentException("For some reason the more comment parent wasn't a proper comment");
                }
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

        public async void OnMoreClicked(object? sender, EventArgs e)
        {
            if (!string.Equals(_redditClient.LoggedInUser, _comment.Author, StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<CommentMoreOptions, string> overrides = [];

                overrides.Add(CommentMoreOptions.BlockAuthor, $"Block /u/{_comment.Author}");
                overrides.Add(CommentMoreOptions.ViewAuthor, $"View /u/{_comment.Author}");

                if (_comment.Saved == true)
                {
                    overrides.Add(CommentMoreOptions.Save, $"Unsave");
                }

                CommentMoreOptions? postMoreOptions = await this.DisplayActionSheet<CommentMoreOptions>("Select:", null, null, overrides);

                if (postMoreOptions is null)
                {
                    return;
                }

                switch (postMoreOptions.Value)
                {
                    case CommentMoreOptions.Save:
                        if (_comment.Saved == true)
                        {
                            await _redditClient.ToggleSave(_comment, false);
                            _comment.Saved = false;
                        }
                        else
                        {
                            await _redditClient.ToggleSave(_comment, true);
                            _comment.Saved = true;
                        }

                        break;

                    case CommentMoreOptions.BlockAuthor:
                        await this.NewBlockRule(new BlockRule()
                        {
                            Author = Post.Author,
                            BlockType = BlockType.Post,
                            RuleName = $"/u/{Post.Author}"
                        });
                        break;

                    case CommentMoreOptions.ViewAuthor:
                        Ensure.NotNull(_comment.Author);
                        await AppNavigator.OpenUser(_comment.Author);
                        break;

                    case CommentMoreOptions.CopyRaw:
                        await Clipboard.SetTextAsync(_comment.Body);
                        break;

                    case CommentMoreOptions.CopyPermalink:
                        await Clipboard.SetTextAsync(_comment.Permalink);
                        break;
                }
            }
            else
            {
                Dictionary<MyCommentMoreOptions, string> overrideText = [];
                bool replyState = _comment.SendReplies != false;
                overrideText.Add(MyCommentMoreOptions.ToggleReplies, $"{(replyState ? "Disable" : "Enable")} Replies");

                MyCommentMoreOptions? postMoreOptions = await this.DisplayActionSheet("Select:", null, null, overrideText);

                if (postMoreOptions is null)
                {
                    return;
                }

                switch (postMoreOptions.Value)
                {
                    case MyCommentMoreOptions.ToggleReplies:
                        await _redditClient.ToggleInboxReplies(_comment, !replyState);
                        _comment.SendReplies = !replyState;
                        break;

                    case MyCommentMoreOptions.Delete:
                        await _redditClient.Delete(_comment);
                        OnDelete?.Invoke(this, new OnDeleteClickedEventArgs(_comment, this));
                        break;

                    case MyCommentMoreOptions.Edit:
                        ReplyPage replyPage = await AppNavigator.OpenEditPage(_comment);
                        replyPage.OnSubmitted += this.EditPage_OnSubmitted;
                        break;

                    default: throw new EnumNotImplementedException(postMoreOptions.Value);
                }
            }
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

        void ISelectionGroupItem.Select()
        {
            _topBar = new RedditCommentComponentTopBar(_applicationStyling);
            commentContainer.Children.Insert(0, _topBar);

            _bottomBar = new RedditCommentComponentBottomBar(_comment, _applicationStyling);

            _topBar.DoneClicked += this.OnDoneClicked;
            _topBar.HideClicked += this.OnHideClicked;
            _topBar.ParentClicked += this.OnParentClicked;

            _bottomBar.DownvoteClicked += this.OnDownvoteClicked;
            _bottomBar.MoreClicked += this.OnMoreClicked;
            _bottomBar.ReplyClicked += this.OnReplyClicked;
            _bottomBar.UpvoteClicked += this.OnUpvoteClicked;

            commentHeader.BackgroundColor = _applicationStyling.HighlightColor.ToMauiColor();
            commentBody.BackgroundColor = _applicationStyling.HighlightColor.ToMauiColor();

            int indexOfComment = commentContainer.Children.IndexOf(commentBody);

            commentContainer.Children.InsertAfter(commentBody, _bottomBar);
        }

        void ISelectionGroupItem.Unselect()
        {
            commentContainer.Children.Remove(_topBar);
            _topBar = null;
            commentContainer.Children.Remove(_bottomBar);
            _bottomBar = null;
            commentHeader.BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
            commentBody.BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
        }

        internal void LoadImages(bool recursive = false)
        {
            if (commentBody is MarkdownView mv)
            {
                foreach (LinkSpan linkSpan in mv.LinkSpans)
                {
                    Grid? grid = linkSpan.Element.Closest<Grid>();

                    Label? label = linkSpan.Element.Closest<Label>();

                    if (grid is null || label is null)
                    {
                        Debug.WriteLine("Could not find image grid or label");
                        continue;
                    }

                    PostItems item = UrlHelper.Resolve(linkSpan.Url);

                    if (item.Kind == PostTargetKind.Image)
                    {
                        grid.Children.InsertAfter(
                            label,
                            new Image()
                            {
                                Source = ImageSource.FromStream(async (c) => await this.GetImageStream(c, linkSpan.Url)),
                                MaximumWidthRequest = commentBody.Width,
                                Aspect = Aspect.AspectFit,
                                MaximumHeightRequest = Application.Current.Windows[0].Height
                            });

                        grid.Children.Remove(label);
                    }
                }
            }

            if (recursive && _replies != null)
            {
                foreach (RedditCommentComponent element in _replies.OfType<RedditCommentComponent>())
                {
                    element.LoadImages(true);
                }
            }
        }

        private void BlockRuleOnSave(object? sender, ObjectEditorSaveEventArgs e)
        {
        }

        private async Task ContinueThread(ApiComment comment)
        {
            await AppNavigator.OpenPost(Post, comment);
        }

        private async void EditPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            _comment.Body = e.NewComment.Body;

            if (commentBody is Label label)
            {
                label.Text = MarkDownHelper.Clean(_comment.Body);
            }
            else if (commentBody is MarkdownView markdownView)
            {
                markdownView.MarkdownText = MarkDownHelper.Clean(_comment.Body);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private async Task<Stream> GetImageStream(CancellationToken c, string url)
        {
            if (!_cachedImageStreams.TryGetValue(url, out Stream cachedImageStream))
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    cachedImageStream = await ImageHelper.ResizeLargeImageWithContainFitAsync(url,
                                                                                              (int)Application.Current.Windows[0].Width,
                                                                                              (int)Application.Current.Windows[0].Height);
                    _cachedImageStreams.Add(url, cachedImageStream);
                }
            }

            if (cachedImageStream is null)
            {
                return null;
            }

            cachedImageStream.Seek(0, SeekOrigin.Begin);

            return cachedImageStream;
        }

        private async Task LoadMoreCommentsAsync(IMore comment)
        {
            List<ApiThing> response = await _redditClient.MoreComments(Post, comment);

            this.AddChildren(response, true);
        }

        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = await AppNavigator.OpenObjectEditor(blockRule);

            objectEditorPage.OnSave += this.BlockRuleOnSave;
        }

        private async void OnShareClicked(object? sender, EventArgs e)
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = _comment.Permalink,
                Title = _comment.Body
            });
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