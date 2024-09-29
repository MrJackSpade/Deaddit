using Deaddit.Components.WebComponents.Partials.Comment;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
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
using System.Web;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("reddit-comment")]
    public class RedditCommentWebComponent : DivComponent, IHasChildren, ISelectionGroupItem
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly BottomBarComponent _bottomBar;

        private readonly ApiComment _comment;

        private readonly HtmlBodyComponent _commentBody;

        private readonly DivComponent _commentContainer;

        private readonly CommentHeaderComponent _commentHeader;

        private readonly INavigation _navigation;

        private readonly IRedditClient _redditClient;

        private readonly RepliesContainerComponent _replies;

        private readonly TopBarComponent _topBar;

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

            _commentBody = new HtmlBodyComponent(comment.BodyHtml, applicationStyling);

            _replies = new RepliesContainerComponent(_applicationStyling);

            _commentHeader = new CommentHeaderComponent(applicationStyling, comment, post);

            SpanComponent authorSpan = new()
            {
                InnerText = comment.Author,
                Color = _applicationStyling.SubTextColor.ToHex(),
                MarginRight = "5px"
            };

            _topBar = new TopBarComponent();

            _bottomBar = new BottomBarComponent(applicationStyling);

            _bottomBar.OnUpvoteClicked += this.OnUpvoteClicked;
            _bottomBar.OnDownvoteClicked += this.OnDownvoteClicked;
            _bottomBar.OnMoreClicked += this.OnMoreClicked;
            _bottomBar.OnShareClicked += this.OnShareClicked;
            _bottomBar.OnReplyClicked += this.OnReplyClicked;

            _commentContainer.Children.Add(_topBar);
            _commentContainer.Children.Add(_commentHeader);
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
                _commentHeader.SetIndicatorState(UpvoteState.None);
                _redditClient.SetUpvoteState(_comment, UpvoteState.None);
            }
            else if (_comment.Likes == UpvoteState.Downvote)
            {
                _comment.Score += 2;
                _commentHeader.SetIndicatorState(UpvoteState.Upvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Upvote);
            }
            else
            {
                _comment.Score++;
                _commentHeader.SetIndicatorState(UpvoteState.Upvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Upvote);
            }
        }

        public void Select()
        {
            _commentContainer.BackgroundColor = _applicationStyling.HighlightColor.ToHex();
            _topBar.Display = "flex";
            _bottomBar.Display = "flex";
            _replies.BorderLeft = $"1px solid {_applicationStyling.HighlightColor.ToHex()}";
        }

        public void Unselect()
        {
            _commentContainer.BackgroundColor = null;
            _topBar.Display = "none";
            _bottomBar.Display = "none";
            _replies.BorderLeft = $"1px solid {_applicationStyling.TextColor.ToHex()}";
        }

        internal void LoadImages(bool recursive = false)
        {
            // Parse the InnerText as HTML
            HtmlAgilityPack.HtmlDocument doc = new();
            doc.LoadHtml(_commentBody.InnerHTML);

            bool modified = false;

            // Find all <a> elements with href attributes
            HtmlAgilityPack.HtmlNodeCollection links = doc.DocumentNode.SelectNodes("//a[@href]");
            if (links != null)
            {
                foreach (HtmlAgilityPack.HtmlNode? link in links)
                {
                    string href = HttpUtility.HtmlDecode(link.GetAttributeValue("href", string.Empty));
                    // Use UrlHelper.Resolve to determine if it's an image
                    PostItems items = UrlHelper.Resolve(href);
                    if (items.Kind == PostTargetKind.Image)
                    {
                        modified = true;
                        // Create a new <img> element
                        HtmlAgilityPack.HtmlNode img = HtmlAgilityPack.HtmlNode.CreateNode($"<a href=\"{href}\"><img src=\"{href}\" /></a>");
                        // Replace the <a> element with the <img> element
                        link.ParentNode.ReplaceChild(img, link);
                    }
                }
            }

            if (modified)
            {
                // Update the InnerText with the modified HTML
                _commentBody.InnerHTML = doc.DocumentNode.InnerHtml;
            }

            if (recursive)
            {
                foreach (RedditCommentWebComponent redditCommentWebComponent in _replies.Children.OfType<RedditCommentWebComponent>())
                {
                    redditCommentWebComponent.LoadImages(true);
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

        private void EditPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
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
                _commentHeader.SetIndicatorState(UpvoteState.None);
                _redditClient.SetUpvoteState(_comment, UpvoteState.None);
            }
            else if (_comment.Likes == UpvoteState.Upvote)
            {
                _comment.Score -= 2;
                _commentHeader.SetIndicatorState(UpvoteState.Downvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Downvote);
            }
            else
            {
                _comment.Score--;
                _commentHeader.SetIndicatorState(UpvoteState.Downvote);
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
    }
}