using Deaddit.Components;
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
using Deaddit.MAUI.Components.Partials;
using Deaddit.Pages;
using Deaddit.Utils;

namespace Deaddit.MAUI.Components
{
    public partial class RedditCommentComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly ApiComment _comment;

        private readonly SelectionGroup _commentSelectionGroup;

        private readonly ApiPost _post;

        private readonly IRedditClient _redditClient;

        private readonly View commentBody;

        private RedditCommentComponentBottomBar? _bottomBar;

        private VerticalStackLayout? _replies;

        private RedditCommentComponentTopBar? _topBar;

        public RedditCommentComponent(ApiComment comment, ApiPost post, bool selectEnabled, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration)
        {
            SelectEnabled = selectEnabled;
            _applicationStyling = applicationTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _post = post;
            _appNavigator = appNavigator;
            _comment = comment;
            _commentSelectionGroup = selectionTracker;

            this.InitializeComponent();

            if (comment.Distinguished == DistinguishedKind.Moderator)
            {
                authorLabel.TextColor = applicationTheme.DistinguishedAuthorTextColor.ToMauiColor();
                authorLabel.BackgroundColor = applicationTheme.DistinguishedAuthorBackgroundColor.ToMauiColor();
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

        public event EventHandler<OnDeleteClickedEventArgs>? OnDelete;

        public bool SelectEnabled { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="children"></param>
        /// <param name="loadMore">Force load collapsed children</param>
        /// <exception cref="NotImplementedException"></exception>
        public void AddChildren(IEnumerable<ApiThing> children, bool loadMore = false)
        {
            foreach (ApiThing? child in children)
            {
                ApiThing renderChild = child;

                if (!_blockConfiguration.IsAllowed(renderChild))
                {
                    continue;
                }

                if (renderChild.Id == _post.Id)
                {
                    continue;
                }

                if (renderChild.IsDeleted() || renderChild.IsRemoved())
                {
                    continue;
                }

                ContentView? childComponent = null;

                if (renderChild is ApiComment comment)
                {
                    if (comment.CollapsedReasonCode == CollapsedReasonKind.None || loadMore)
                    {
                        RedditCommentComponent commentComponent = _appNavigator.CreateCommentComponent(comment, _post, _commentSelectionGroup);
                        commentComponent.AddChildren(comment.GetReplies());
                        commentComponent.OnDelete += this.OnCommentDelete;
                        childComponent = commentComponent;
                    } else
                    {
                        renderChild = new CollapsedMore(comment);
                    }
                }
                
                if (renderChild is IMore more)
                {
                    MoreCommentsComponent mcomponent = _appNavigator.CreateMoreCommentsComponent(more);
                    mcomponent.OnClick += this.MoreCommentsClick;
                    childComponent = mcomponent;
                }

                if(childComponent is null)
                {
                    throw new NotImplementedException();
                }

                this.TryInitReplies();
                _replies.Add(childComponent);
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

            PostItems resource = RedditPostExtensions.Resolve(e.Url);

            await Navigation.OpenResource(resource, _appNavigator);
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

                CommentMoreOptions? postMoreOptions = await this.DisplayActionSheet<CommentMoreOptions>("Select:", null, null);

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
                            Author = _post.Author,
                            BlockType = BlockType.Post,
                            RuleName = $"/u/{_post.Author}"
                        });
                        break;

                    case CommentMoreOptions.ViewAuthor:
                        Ensure.NotNull(_comment.Author);
                        await _appNavigator.OpenUser(_comment.Author);
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
                        ReplyPage replyPage = await _appNavigator.OpenEditPage(_comment);
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
            _commentSelectionGroup.Toggle(this);
        }

        public async void OnReplyClicked(object? sender, EventArgs e)
        {
            ReplyPage replyPage = await _appNavigator.OpenReplyPage(_comment);
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

        private void BlockRuleOnSave(object? sender, ObjectEditorSaveEventArgs e)
        {
        }

        private async Task ContinueThread(ApiComment comment)
        {
            await _appNavigator.OpenPost(_post, comment);
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

        private async Task LoadMoreCommentsAsync(IMore comment)
        {
            List<ApiThing> response = await _redditClient.MoreComments(_post, comment).ToList();

            this.AddChildren(response, true);
        }

        private async void MoreCommentsClick(object? sender, IMore e)
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

        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = await _appNavigator.OpenObjectEditor(blockRule);

            objectEditorPage.OnSave += this.BlockRuleOnSave;
        }

        private void OnCommentDelete(object? sender, OnDeleteClickedEventArgs e)
        {
            _replies.Children.Remove(e.Component);
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

            RedditCommentComponent redditCommentComponent = _appNavigator.CreateCommentComponent(e.NewComment, _post, _commentSelectionGroup);
            redditCommentComponent.OnDelete += this.OnCommentDelete;
            this.TryInitReplies();
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

        private void TryInitReplies()
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