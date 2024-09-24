using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
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
    public class RedditCommentWebComponent : DivComponent, IHasChildren
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly ApiComment _comment;

        private readonly DivComponent _commentBody;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly DivComponent _replies;

        private readonly bool _selectEnabled;

        private readonly SelectionGroup _selectionGroup;

        public IAppNavigator AppNavigator { get; }

        public BlockConfiguration BlockConfiguration { get; }

        WebComponent IHasChildren.ChildContainer => _replies;

        public ApiPost? Post { get; }

        public SelectionGroup SelectionGroup { get; }

        public event EventHandler<OnDeleteClickedEventArgs> OnDelete;

        public RedditCommentWebComponent(ApiComment comment, ApiPost? post, bool selectEnabled, INavigation navigation, AppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup selectionGroup, BlockConfiguration blockConfiguration)
        {
            _comment = comment;
            Post = post;
            _selectEnabled = selectEnabled;
            AppNavigator = appNavigator;
            _redditClient = redditClient;
            _applicationStyling = applicationStyling;
            _selectionGroup = selectionGroup;
            BlockConfiguration = blockConfiguration;
            _navigation = navigation;

            _commentBody = new DivComponent()
            {
                InnerText = comment.BodyHtml
            };

            _replies = new DivComponent();

            this.Children.Add(_commentBody);
            this.Children.Add(_replies);
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

        internal void LoadImages(bool recursive = false)
        {
            throw new NotImplementedException();
        }

        private void BlockRuleOnSave(object? sender, ObjectEditorSaveEventArgs e)
        {
            throw new NotImplementedException();
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

        private async void OnShareClicked(object? sender, EventArgs e)
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = _comment.Permalink,
                Title = _comment.Body
            });
        }
    }
}