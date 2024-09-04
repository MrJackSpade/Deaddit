using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Exceptions;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components.ComponentModels;
using Deaddit.MAUI.Components.Partials;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Extensions;
using Deaddit.MAUI.Pages;
using Deaddit.Reddit.Extensions;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Reddit.Models.Options;
using Deaddit.Services;
using Deaddit.Utils;
using Deaddit.Utils.Extensions;
using Microsoft.Maui.Platform;
using System.Diagnostics.CodeAnalysis;

namespace Deaddit.MAUI.Components
{
    public partial class RedditCommentComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly RedditCommentComponentViewModel _commentViewModel;

        private VerticalStackLayout _replies;

        private readonly IConfigurationService _configurationService;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _commentSelectionGroup;

        private readonly IVisitTracker _visitTracker;

        private readonly RedditComment _comment;

        private readonly RedditPost _post;

        private RedditCommentComponentTopBar _topBar { get; set; }

        private RedditCommentComponentBottomBar _bottomBar { get; set; }

        private RedditCommentComponent(RedditComment comment, RedditPost post, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _applicationTheme = applicationTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _post = post;
            _comment = comment;
            _visitTracker = visitTracker;
            _configurationService = configurationService;
            _commentSelectionGroup = selectionTracker;

            BindingContext = _commentViewModel = new RedditCommentComponentViewModel(comment, applicationTheme);
            this.InitializeComponent();



            if (MarkDownHelper.IsMarkDown(_comment.Body))
            {          
                int markdownIndex = commentBody.Children.IndexOf(contentLabel);
                commentBody.Children.RemoveAt(markdownIndex);

                // Content Text as Markdown
                var markdownView = new MarkdownView
                {
                    MarkdownText = _commentViewModel.Content,
                    HyperlinkColor = _commentViewModel.HyperlinkColor,
                    TextColor = _commentViewModel.TextColor,
                    TextFontSize = _commentViewModel.FontSize,
                    BlockQuoteBorderColor = _commentViewModel.TextColor,
                    BlockQuoteBackgroundColor = _commentViewModel.SecondaryColor,
                    BlockQuoteTextColor = _commentViewModel.TextColor,
                    Margin = new Thickness(15, 0, 0, 0)
                };
                markdownView.OnHyperLinkClicked += this.MarkdownView_OnHyperLinkClicked;

                // Add to the layout
                commentBody.Children.Insert(markdownIndex,markdownView);
            }
        }

        public bool SelectEnabled { get; private set; }

        public static RedditCommentComponent FullView(RedditComment comment, RedditPost post, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditCommentComponent toReturn = new(comment, post, redditClient, applicationTheme, visitTracker, selectionTracker, blockConfiguration, configurationService)
            {
                SelectEnabled = true
            };

            return toReturn;
        }

        public static RedditCommentComponent Preview(RedditComment comment, RedditPost post, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditCommentComponent toReturn = new(comment, post, redditClient, applicationTheme, visitTracker, selectionTracker, blockConfiguration, configurationService)
            {
                SelectEnabled = false
            };

            return toReturn;
        }

        private void TryInitReplies()
        {
            if (_replies is null)
            {
                _replies = new VerticalStackLayout()
                {
                    VerticalOptions = LayoutOptions.Fill,
                    Margin = new Thickness(20, 0, 0, 0),
                    BackgroundColor = _applicationTheme.TertiaryColor
                };

                commentContainer.Children.Add(_replies);
            }
        }

        public void AddChildren(IEnumerable<RedditCommentMeta> children)
        {
            foreach (RedditCommentMeta? child in children)
            {
                if (child?.Data is null)
                {
                    continue;
                }

                if (!_blockConfiguration.BlockRules.IsAllowed(child.Data))
                {
                    continue;
                }

                if (child.Data.Id == _post.Id)
                {
                    continue;
                }

                ContentView childComponent = null;

                switch (child.Kind)
                {
                    case ThingKind.Comment:
                        RedditCommentComponent ccomponent = FullView(child.Data, _post, _redditClient, _applicationTheme, _visitTracker, _commentSelectionGroup, _blockConfiguration, _configurationService);
                        ccomponent.AddChildren(child.GetReplies());
                        childComponent = ccomponent;
                        break;
                    case ThingKind.More:
                        MoreCommentsComponent mcomponent = new(child.Data, _applicationTheme);
                        mcomponent.OnClick += this.MoreCommentsClick;
                        childComponent = mcomponent;
                        break;
                    default:
                        throw new UnhandledEnumException(child.Kind);
                }

                this.TryInitReplies();
                _replies.Add(childComponent);
            }
        }

        private async void MoreCommentsClick(object? sender, RedditComment e)
        {
            MoreCommentsComponent mcomponent = sender as MoreCommentsComponent;

            await DataService.LoadAsync(_replies, async () => await this.LoadDataAsync(e), _applicationTheme.HighlightColor);

            _replies.Remove(mcomponent);
        }

        public void OnDownvoteClicked(object sender, EventArgs e)
        {
            if (_comment.Likes == UpvoteState.Downvote)
            {
                _comment.Likes = UpvoteState.None;
                _commentViewModel.TryAdjustScore(1);
            }
            else
            {
                _comment.Likes = UpvoteState.Downvote;
                _commentViewModel.TryAdjustScore(-1);
            }

            _commentViewModel.SetUpvoteState(_comment.Likes);
            _redditClient.SetUpvoteState(_comment, _comment.Likes);
        }

        private async Task LoadDataAsync(RedditComment comment)
        {
            List<RedditCommentMeta> response = await _redditClient.MoreComments(_post, comment).ToList();

            this.AddChildren(response);
        }

        public async void OnReplyClicked(object sender, EventArgs e)
        {
            ReplyPage replyPage = new(_comment, _redditClient, _applicationTheme, _visitTracker, _blockConfiguration, _configurationService);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
            await Navigation.PushAsync(replyPage);
        }

        public void OnUpvoteClicked(object sender, EventArgs e)
        {
            if (_comment.Likes == UpvoteState.Upvote)
            {
                _comment.Likes = UpvoteState.None;
                _commentViewModel.TryAdjustScore(-1);
            }
            else
            {
                _comment.Likes = UpvoteState.Upvote;
                _commentViewModel.TryAdjustScore(1);
            }

            _commentViewModel.SetUpvoteState(_comment.Likes);
            _redditClient.SetUpvoteState(_comment, _comment.Likes);
        }

        void ISelectionGroupItem.Select()
        {
            _topBar = new RedditCommentComponentTopBar(_comment, _applicationTheme);
            topBarPlaceholder.Children.Add(_topBar);
            _bottomBar = new RedditCommentComponentBottomBar(_comment, _applicationTheme);
            bottomBarPlaceholder.Children.Add(_bottomBar);

            _topBar.DoneClicked += this.OnDoneClicked;
            _topBar.HideClicked += this.OnHideClicked;
            _topBar.ParentClicked += this.OnParentClicked;

            _bottomBar.DownvoteClicked += this.OnDownvoteClicked;
            _bottomBar.MoreClicked += this.OnMoreClicked;
            _bottomBar.ReplyClicked += this.OnReplyClicked;
            _bottomBar.UpvoteClicked += this.OnUpvoteClicked;

            commentBody.BackgroundColor = _applicationTheme.HighlightColor;
        }

        void ISelectionGroupItem.Unselect()
        {
            topBarPlaceholder.Children.Remove(_topBar);
            _topBar = null;
            bottomBarPlaceholder.Children.Remove(_bottomBar);
            _bottomBar = null;
            commentBody.BackgroundColor = _applicationTheme.SecondaryColor;
        }

        public async void MarkdownView_OnHyperLinkClicked(object sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);

            PostTarget resource = UrlHandler.Resolve(e.Url);

            await Navigation.OpenResource(resource, _redditClient, _applicationTheme, _visitTracker, _blockConfiguration, _configurationService);
        }

        public void OnDoneClicked(object sender, EventArgs e)
        {
            // Handle Done click
        }

        public void OnHideClicked(object sender, EventArgs e)
        {
            // Handle Hide click
        }
        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = new(blockRule, _applicationTheme);

            objectEditorPage.OnSave += this.BlockRuleOnSave;

            await Navigation.PushAsync(objectEditorPage);
        }

        private void BlockRuleOnSave(object? sender, ObjectEditorSaveEventArgs e)
        {

        }

        public async void OnMoreClicked(object sender, EventArgs e)
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
                }
            }
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public void OnMoreSelect(object? sender, string e)
        {
        }

        public void OnParentClicked(object sender, EventArgs e)
        {
            // Handle Parent click
        }

        public void OnParentTapped(object sender, TappedEventArgs e)
        {
            _commentSelectionGroup.Toggle(this);
        }

        private void OnShareClicked(object sender, EventArgs e)
        {
        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment.Data, "New comment data");

            RedditCommentComponent redditCommentComponent = FullView(e.NewComment.Data, _post, _redditClient, _applicationTheme, _visitTracker, _commentSelectionGroup, _blockConfiguration, _configurationService);

            this.TryInitReplies();
            _replies.Children.Insert(0, redditCommentComponent);
        }
    }
}