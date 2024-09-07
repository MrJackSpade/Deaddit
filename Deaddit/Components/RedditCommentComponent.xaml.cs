﻿using Deaddit.Components;
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
using Deaddit.Core.Utils.Extensions;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.MAUI.Components.Partials;
using Deaddit.Pages;
using Deaddit.Utils;

namespace Deaddit.MAUI.Components
{
    public partial class RedditCommentComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly ApiComment _comment;

        private readonly SelectionGroup _commentSelectionGroup;

        private readonly IConfigurationService _configurationService;

        private readonly ApiPost _post;

        private readonly IRedditClient _redditClient;

        private readonly IVisitTracker _visitTracker;

        private readonly VisualElement commentBody;

        private RedditCommentComponentBottomBar? _bottomBar;

        private VerticalStackLayout? _replies;

        private RedditCommentComponentTopBar? _topBar;

        private RedditCommentComponent(ApiComment comment, ApiPost post, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _applicationTheme = applicationTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _post = post;
            _applicationHacks = applicationHacks;
            _comment = comment;
            _visitTracker = visitTracker;
            _configurationService = configurationService;
            _commentSelectionGroup = selectionTracker;

            this.InitializeComponent();

            //--------- SPEED
            if (comment.Distinguished == DistinguishedKind.Moderator)
            {
                authorLabel.TextColor = applicationTheme.DistinguishedColor.ToMauiColor();
            }
            else
            {
                authorLabel.TextColor = applicationTheme.TertiaryColor.ToMauiColor();
            }

            authorLabel.Text = comment.Author;

            commentContainer.Background = applicationTheme.SecondaryColor.ToMauiColor();
            //----------

            if (MarkDownHelper.IsMarkDown(_comment.Body))
            {
                int markdownIndex = commentContainer.Children.IndexOf(contentLabel);
                commentContainer.Children.RemoveAt(markdownIndex);

                // Content Text as Markdown
                MarkdownView markdownView = new()
                {
                    MarkdownText = MarkDownHelper.Clean(_comment.Body),
                    HyperlinkColor = _applicationTheme.HyperlinkColor.ToMauiColor(),
                    TextColor = _applicationTheme.TextColor.ToMauiColor(),
                    TextFontSize = _applicationTheme.FontSize,
                    BlockQuoteBorderColor = _applicationTheme.TextColor.ToMauiColor(),
                    BlockQuoteBackgroundColor = _applicationTheme.SecondaryColor.ToMauiColor(),
                    BlockQuoteTextColor = _applicationTheme.TextColor.ToMauiColor(),
                    Padding = new Thickness(15, 0, 0, 10)
                };

                markdownView.OnHyperLinkClicked += this.OnHyperLinkClicked;

                // Add to the layout
                commentContainer.Children.Insert(markdownIndex, markdownView);

                commentBody = markdownView;
            }
            else
            {
                contentLabel.Text = MarkDownHelper.Clean(_comment.Body);
                contentLabel.TextColor = _applicationTheme.TextColor.ToMauiColor();
                contentLabel.FontSize = _applicationTheme.FontSize;
                commentBody = contentLabel;
            }

            metaDataLabel.TextColor = _applicationTheme.SubTextColor.ToMauiColor();
            metaDataLabel.FontSize = _applicationTheme.FontSize * 0.75;

            this.SetIndicatorState(_comment.Likes);

            this.UpdateMetaData();
        }

        public event EventHandler<OnDeleteClickedEventArgs>? OnDelete;

        public bool SelectEnabled { get; private set; }

        public static RedditCommentComponent FullView(ApiComment comment, ApiPost post, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditCommentComponent toReturn = new(comment, post, redditClient, applicationTheme, applicationHacks, visitTracker, selectionTracker, blockConfiguration, configurationService)
            {
                SelectEnabled = true
            };

            return toReturn;
        }

        public static RedditCommentComponent Preview(ApiComment comment, ApiPost post, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditCommentComponent toReturn = new(comment, post, redditClient, applicationTheme, applicationHacks, visitTracker, selectionTracker, blockConfiguration, configurationService)
            {
                SelectEnabled = false
            };

            return toReturn;
        }

        public void AddChildren(IEnumerable<ApiThing> children)
        {
            foreach (ApiThing? child in children)
            {
                if (!_blockConfiguration.BlockRules.IsAllowed(child))
                {
                    continue;
                }

                if (child.Id == _post.Id)
                {
                    continue;
                }

                if (child.IsDeleted() || child.IsRemoved())
                {
                    continue;
                }

                ContentView? childComponent = null;

                if (child is ApiComment comment)
                {
                    RedditCommentComponent ccomponent = RedditCommentComponent.FullView(comment, _post, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _commentSelectionGroup, _blockConfiguration, _configurationService);
                    ccomponent.AddChildren(comment.GetReplies());
                    ccomponent.OnDelete += this.OnCommentDelete;
                    childComponent = ccomponent;
                }
                else if (child is ApiMore more)
                {
                    MoreCommentsComponent mcomponent = new(more, _applicationTheme);
                    mcomponent.OnClick += this.MoreCommentsClick;
                    childComponent = mcomponent;
                }
                else
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

            await Navigation.OpenResource(resource, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
        }

        public async void OnMoreClicked(object? sender, EventArgs e)
        {
            if (!string.Equals(_redditClient.LoggedInUser, _comment.Author, StringComparison.OrdinalIgnoreCase))
            {
                CommentMoreOptions? postMoreOptions = await this.DisplayActionSheet<CommentMoreOptions>("Select:", null, null);

                if (postMoreOptions is null)
                {
                    return;
                }

                switch (postMoreOptions.Value)
                {
                    case CommentMoreOptions.BlockAuthor:
                        await this.NewBlockRule(new BlockRule()
                        {
                            Author = _post.Author,
                            BlockType = BlockType.Post,
                            RuleName = $"/u/{_post.Author}"
                        });
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
            ReplyPage replyPage = new(_comment, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
            await Navigation.PushAsync(replyPage);
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
            _topBar = new RedditCommentComponentTopBar(_comment, _applicationTheme);
            commentContainer.Children.Insert(0, _topBar);

            _bottomBar = new RedditCommentComponentBottomBar(_comment, _applicationTheme);

            _topBar.DoneClicked += this.OnDoneClicked;
            _topBar.HideClicked += this.OnHideClicked;
            _topBar.ParentClicked += this.OnParentClicked;

            _bottomBar.DownvoteClicked += this.OnDownvoteClicked;
            _bottomBar.MoreClicked += this.OnMoreClicked;
            _bottomBar.ReplyClicked += this.OnReplyClicked;
            _bottomBar.UpvoteClicked += this.OnUpvoteClicked;

            commentHeader.BackgroundColor = _applicationTheme.HighlightColor.ToMauiColor();
            commentBody.BackgroundColor = _applicationTheme.HighlightColor.ToMauiColor();

            int indexOfComment = commentContainer.Children.IndexOf(commentBody);

            commentContainer.Children.InsertAfter(commentBody, _bottomBar);
        }

        void ISelectionGroupItem.Unselect()
        {
            commentContainer.Children.Remove(_topBar);
            _topBar = null;
            commentContainer.Children.Remove(_bottomBar);
            _bottomBar = null;
            commentHeader.BackgroundColor = _applicationTheme.SecondaryColor.ToMauiColor();
            commentBody.BackgroundColor = _applicationTheme.SecondaryColor.ToMauiColor();
        }

        private void BlockRuleOnSave(object? sender, ObjectEditorSaveEventArgs e)
        {
        }

        private async Task ContinueThread(ApiComment comment)
        {
            PostPage redditPostPage = new(_post, comment, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);

            await redditPostPage.TryLoad();

            await Navigation.PushAsync(redditPostPage);
        }

        private async Task LoadMoreCommentsAsync(ApiMore comment)
        {
            List<ApiThing> response = await _redditClient.MoreComments(_post, comment).ToList();

            this.AddChildren(response);
        }

        private async void MoreCommentsClick(object? sender, ApiMore e)
        {
            MoreCommentsComponent? mcomponent = sender as MoreCommentsComponent;

            if (e.ChildNames.NotNullAny())
            {
                await DataService.LoadAsync(_replies, async () => await this.LoadMoreCommentsAsync(e), _applicationTheme.HighlightColor.ToMauiColor());

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
            ObjectEditorPage objectEditorPage = new(blockRule, _applicationTheme);

            objectEditorPage.OnSave += this.BlockRuleOnSave;

            await Navigation.PushAsync(objectEditorPage);
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

            RedditCommentComponent redditCommentComponent = FullView(e.NewComment, _post, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _commentSelectionGroup, _blockConfiguration, _configurationService);
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
                    voteIndicator.TextColor = _applicationTheme.UpvoteColor.ToMauiColor();
                    voteIndicator.IsVisible = true;
                    break;

                case UpvoteState.Downvote:
                    _comment.Likes = UpvoteState.Downvote;
                    voteIndicator.Text = "▼";
                    voteIndicator.TextColor = _applicationTheme.DownvoteColor.ToMauiColor();
                    voteIndicator.IsVisible = true;
                    break;

                default:
                    _comment.Likes = UpvoteState.None;
                    voteIndicator.Text = string.Empty;
                    voteIndicator.TextColor = _applicationTheme.TextColor.ToMauiColor();
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
                    BackgroundColor = _applicationTheme.TertiaryColor.ToMauiColor(),
                    Padding = new Thickness(1, 0, 0, 0)
                };

                commentContainer.Padding = new Thickness(0, 0, 0, 6);

                commentContainer.Add(_replies);
            }
        }

        private void UpdateMetaData()
        {
            metaDataLabel.Text = $"{_comment.Score} points {_comment.CreatedUtc.Elapsed()}";
        }
    }
}