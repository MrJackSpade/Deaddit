using Deaddit.Components.WebComponents.Partials.Comment;
using Deaddit.Components.WebComponents.Partials.Message;
using Deaddit.Components.WebComponents.Partials.Post;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.Options;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Pages;
using Deaddit.Utils;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("reddit-message")]
    public class RedditMessageWebComponent : DivComponent, ISelectionGroupItem
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly MessageBarComponent _bottomBar;

        private readonly HtmlBodyComponent _commentBody;

        private readonly DivComponent _commentContainer;

        private readonly ApiMessage _message;

        private readonly MessageHeaderComponent _messageHeader;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly RepliesContainerComponent _replies;

        private readonly TopBarComponent _topBar;

        public IAppNavigator AppNavigator { get; }

        public BlockConfiguration BlockConfiguration { get; }

        public ApiPost Post { get; }

        public bool SelectEnabled { get; private set; }

        public SelectionGroup SelectionGroup { get; private set; }

        public event EventHandler<OnDeleteClickedEventArgs> OnDelete;

        public RedditMessageWebComponent(ApiMessage message, ApiPost? post, bool selectEnabled, INavigation navigation, AppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup selectionGroup, BlockConfiguration blockConfiguration)
        {
            _message = message;
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

            _commentBody = new HtmlBodyComponent(message.BodyHtml, applicationStyling);

            _replies = new RepliesContainerComponent(_applicationStyling);

            _messageHeader = new MessageHeaderComponent(applicationStyling, message);

            SpanComponent authorSpan = new()
            {
                InnerText = message.Author,
                Color = _applicationStyling.SubTextColor.ToHex(),
                MarginRight = "5px"
            };

            _topBar = new TopBarComponent();

            _bottomBar = new MessageBarComponent(applicationStyling);

            _bottomBar.OnMoreClicked += this.OnMoreClicked;
            _bottomBar.OnReplyClicked += this.OnReplyClicked;

            _commentContainer.Children.Add(_topBar);
            _commentContainer.Children.Add(_messageHeader);
            _commentContainer.Children.Add(_commentBody);
            _commentContainer.Children.Add(_bottomBar);

            Children.Add(_commentContainer);
            Children.Add(_replies);

            BoxSizing = "border-box";
            PaddingLeft = "5px";
            PaddingTop = "5px";
            PaddingBottom = "5px";

            Display = "flex";
            FlexDirection = "column";

            _commentContainer.OnClick += this.SelectClick;
        }

        public async void OnMoreClicked(object? sender, EventArgs e)
        {
            if (!string.Equals(_redditClient.LoggedInUser, _message.Author, StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<CommentMoreOptions, string> overrides = [];

                overrides.Add(CommentMoreOptions.BlockAuthor, $"Block /u/{_message.Author}");
                overrides.Add(CommentMoreOptions.ViewAuthor, $"View /u/{_message.Author}");

                if (_message.Saved == true)
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
                        if (_message.Saved == true)
                        {
                            await _redditClient.ToggleSave(_message, false);
                            _message.Saved = false;
                        }
                        else
                        {
                            await _redditClient.ToggleSave(_message, true);
                            _message.Saved = true;
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
                        Ensure.NotNull(_message.Author);
                        await AppNavigator.OpenUser(_message.Author);
                        break;

                    case CommentMoreOptions.CopyRaw:
                        await Clipboard.SetTextAsync(_message.Body);
                        break;

                    case CommentMoreOptions.CopyPermalink:
                        await Clipboard.SetTextAsync(_message.Permalink);
                        break;
                }
            }
            else
            {
                Dictionary<MyCommentMoreOptions, string> overrideText = [];
                bool replyState = _message.SendReplies != false;
                overrideText.Add(MyCommentMoreOptions.ToggleReplies, $"{(replyState ? "Disable" : "Enable")} Replies");

                MyCommentMoreOptions? postMoreOptions = await _navigation.NavigationStack[^1].DisplayActionSheet("Select:", null, null, overrideText);

                if (postMoreOptions is null)
                {
                    return;
                }

                switch (postMoreOptions.Value)
                {
                    case MyCommentMoreOptions.ToggleReplies:
                        await _redditClient.ToggleInboxReplies(_message, !replyState);
                        _message.SendReplies = !replyState;
                        break;

                    case MyCommentMoreOptions.Delete:
                        await _redditClient.Delete(_message);
                        OnDelete?.Invoke(this, new OnDeleteClickedEventArgs(_message, this));
                        break;

                    case MyCommentMoreOptions.Edit:
                        ReplyPage replyPage = await AppNavigator.OpenEditPage(_message);
                        replyPage.OnSubmitted += this.EditPage_OnSubmitted;
                        break;

                    default: throw new EnumNotImplementedException(postMoreOptions.Value);
                }
            }
        }

        public async void OnReplyClicked(object? sender, EventArgs e)
        {
            ReplyPage replyPage = await AppNavigator.OpenReplyPage(_message);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
        }

        public void Select()
        {
            _commentContainer.BackgroundColor = _applicationStyling.HighlightColor.ToHex();
            _topBar.Display = "flex";
            _bottomBar.Display = "flex";
        }

        public void Unselect()
        {
            _commentContainer.BackgroundColor = null;
            _topBar.Display = "none";
            _bottomBar.Display = "none";
        }

        internal void LoadImages(bool recursive = false)
        {
            throw new NotImplementedException();
        }

        private void BlockRuleOnSave(object? sender, ObjectEditorSaveEventArgs e)
        {
        }

        private async Task ContinueThread(ApiComment comment)
        {
            await AppNavigator.OpenPost(Post, comment);
        }

        private void EditPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            _message.Body = e.NewComment.Body;

            _commentBody.InnerText = e.NewComment.Body;
        }

        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = await AppNavigator.OpenObjectEditor(blockRule);

            objectEditorPage.OnSave += this.BlockRuleOnSave;
        }

        private void ReplyButton_OnClick(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment, "New comment data");

            e.NewComment.ParentId = _message.Id;
            e.NewComment.Parent = _message;

            RedditCommentWebComponent redditCommentComponent = AppNavigator.CreateCommentWebComponent(e.NewComment, Post, SelectionGroup);
            redditCommentComponent.OnDelete += (s, e) => _replies.Children.Remove(redditCommentComponent);
            _replies.Children.Insert(0, redditCommentComponent);
        }

        private void SelectClick(object? sender, EventArgs e)
        {
            SelectionGroup?.Select(this);
        }
    }
}