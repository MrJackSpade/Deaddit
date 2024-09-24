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
using Deaddit.Pages;
using Deaddit.Utils;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("reddit-comment")]
    public class RedditCommentWebComponent : DivComponent, IHasChildren, ISelectionGroupItem
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly DivComponent _bottomBar;

        private readonly ApiComment _comment;

        private readonly DivComponent _commentBody;

        private readonly DivComponent _commentContainer;

        private readonly DivComponent _commentHeader;

        private readonly SpanComponent _commentMeta;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly DivComponent _replies;

        private readonly DivComponent _topBar;

        private readonly SpanComponent voteIndicator;

        public IAppNavigator AppNavigator { get; }

        public BlockConfiguration BlockConfiguration { get; }

        WebComponent IHasChildren.ChildContainer => _replies;

        public ApiPost Post { get; }

        public bool SelectEnabled { get; private set; }

        public SelectionGroup SelectionGroup { get; private set; }

        public event EventHandler<OnDeleteClickedEventArgs> OnDelete;

        public RedditCommentWebComponent(ApiComment comment, ApiPost? post, bool selectEnabled, INavigation navigation, AppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup selectionGroup, BlockConfiguration blockConfiguration)
        {
            _comment = comment;
            Post = post;
            SelectEnabled = selectEnabled;
            AppNavigator = appNavigator;
            _redditClient = redditClient;
            _applicationStyling = applicationStyling;
            SelectionGroup = selectionGroup;
            BlockConfiguration = blockConfiguration;
            _navigation = navigation;

            _commentContainer = new DivComponent()
            {
                Display = "flex",
                FlexDirection = "column",
            };

            _commentBody = new DivComponent()
            {
                InnerText = comment.BodyHtml,
                Color = _applicationStyling.TextColor.ToHex(),
                FontSize = $"{(int)(_applicationStyling.FontSize * .75)}px",
                PaddingLeft = "10px"
            };

            _replies = new DivComponent()
            {
                PaddingLeft = "25px",
                BorderLeft = $"1px solid {_applicationStyling.SubTextColor.ToHex()}"
            };

            _commentHeader = new DivComponent();

            SpanComponent authorSpan = new()
            {
                InnerText = comment.Author,
                Color = _applicationStyling.SubTextColor.ToHex(),
                MarginRight = "5px"
            };

            _commentMeta = new SpanComponent()
            {
                Color = _applicationStyling.SubTextColor.ToHex(),
                FontSize = $"{(int)(_applicationStyling.FontSize * .75)}px",
            };

            voteIndicator = new SpanComponent();

            _topBar = new DivComponent()
            {
                Display = "none"
            };

            _bottomBar = new DivComponent()
            {
                Display = "none"
            };

            ButtonComponent upvoteButton = this.ActionButton("▲");
            ButtonComponent downvoteButton = this.ActionButton("▼");
            ButtonComponent moreButton = this.ActionButton("...");
            ButtonComponent shareButton = this.ActionButton("⢔");
            ButtonComponent replyButton = this.ActionButton("↩");

            upvoteButton.OnClick += this.OnUpvoteClicked;
            downvoteButton.OnClick += this.OnDownvoteClicked;
            moreButton.OnClick += this.OnMoreClicked;
            shareButton.OnClick += this.OnShareClicked;
            replyButton.OnClick += this.OnReplyClicked;

            _commentHeader.Children.Add(voteIndicator);
            _commentHeader.Children.Add(authorSpan);
            _commentHeader.Children.Add(_commentMeta);
            _commentContainer.Children.Add(_commentHeader);
            _commentContainer.Children.Add(_commentBody);

            Children.Add(_commentContainer);
            Children.Add(_replies);

            BoxSizing = "border-box";
            Padding = "5px";
            Display = "flex";
            FlexDirection = "column";

            _commentContainer.OnClick += this.SelectClick;

            this.UpdateMetaData();
        }

        public async void MoreCommentsClick(object? sender, IMore e)
        {
            MoreCommentsWebComponent? mcomponent = sender as MoreCommentsWebComponent;

            if (e.ChildNames.NotNullAny())
            {
                await DataService.LoadAsync(null, async () => await this.LoadMoreCommentsAsync(e), _applicationStyling.HighlightColor.ToMauiColor());

                _replies.Children.Remove(mcomponent);
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

                CommentMoreOptions? postMoreOptions = await _navigation.NavigationStack[^1].DisplayActionSheet<CommentMoreOptions>("Select:", null, null, overrides);

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

                MyCommentMoreOptions? postMoreOptions = await _navigation.NavigationStack[^1].DisplayActionSheet("Select:", null, null, overrideText);

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

        public void Select()
        {
            _commentContainer.BackgroundColor = _applicationStyling.HighlightColor.ToHex();
        }

        public void Unselect()
        {
            _commentContainer.BackgroundColor = null;
        }

        internal void LoadImages(bool recursive = false)
        {
            throw new NotImplementedException();
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

            _commentBody.InnerText = e.NewComment.Body;
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

        private void OnDownvoteClicked(object? sender, EventArgs e)
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

        private async void OnShareClicked(object? sender, EventArgs e)
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = _comment.Permalink,
                Title = _comment.Body
            });
        }

        private void ReplyButton_OnClick(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment, "New comment data");

            e.NewComment.ParentId = _comment.Id;
            e.NewComment.Parent = _comment;

            RedditCommentWebComponent redditCommentComponent = AppNavigator.CreateCommentWebComponent(e.NewComment, Post, SelectionGroup);
            redditCommentComponent.OnDelete += (s, e) => _replies.Children.Remove(redditCommentComponent);
            _replies.Children.Insert(0, redditCommentComponent);
        }

        private void SelectClick(object? sender, EventArgs e)
        {
            SelectionGroup?.Select(this);
        }

        private void SetIndicatorState(UpvoteState state)
        {
            switch (state)
            {
                case UpvoteState.Upvote:
                    _comment.Likes = UpvoteState.Upvote;
                    voteIndicator.InnerText = "▲";
                    voteIndicator.Color = _applicationStyling.UpvoteColor.ToHex();
                    voteIndicator.Display = "block";
                    break;

                case UpvoteState.Downvote:
                    _comment.Likes = UpvoteState.Downvote;
                    voteIndicator.InnerText = "▼";
                    voteIndicator.Color = _applicationStyling.DownvoteColor.ToHex();
                    voteIndicator.Display = "block";
                    break;

                default:
                    _comment.Likes = UpvoteState.None;
                    voteIndicator.InnerText = string.Empty;
                    voteIndicator.Color = _applicationStyling.TextColor.ToHex();
                    voteIndicator.Display = "none";
                    break;
            }
        }

        private void UpdateMetaData()
        {
            if (!_comment.ScoreHidden == true)
            {
                _commentMeta.InnerText = $"{_comment.Score} points {_comment.CreatedUtc.Elapsed()}";
            }
            else
            {
                _commentMeta.InnerText = _comment.CreatedUtc.Elapsed();
            }
        }
    }
}