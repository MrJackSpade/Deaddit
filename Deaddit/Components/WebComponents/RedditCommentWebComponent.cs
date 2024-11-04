using Deaddit.Components.WebComponents.Partials.Comment;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Reddit.Api.Interfaces;
using Reddit.Api.Models;
using Reddit.Api.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Blocking;
using Deaddit.Core.Utils.Extensions;
using Deaddit.Core.Utils.MultiSelect;
using Deaddit.Core.Utils.Validation;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Pages;
using Deaddit.Utils;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using System.Web;
using Reddit.Api.Extensions;

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

        private readonly MultiSelector _multiselector;

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

        public RedditCommentWebComponent(ApiComment comment, ApiPost? post, bool selectEnabled, ISelectBoxDisplay selectBoxDisplay, INavigation navigation, AppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationStyling, SelectionGroup selectionGroup, BlockConfiguration blockConfiguration)
        {
            _multiselector = new MultiSelector(selectBoxDisplay);
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

            _bottomBar = new BottomBarComponent(applicationStyling, comment.Likes);

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
                await DataService.LoadAsync(async () => await this.LoadMoreCommentsAsync(e));

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
                await _multiselector.Select(
                    "Select:",
                    new($"Block /u/{_comment.Author}", async () => await this.NewBlockRule(BlockRuleHelper.FromAuthor(_comment))),
                    new($"View /u/{_comment.Author}", async () => await AppNavigator.OpenUser(_comment.Author)),
                    new(_comment.Saved == true ? "Unsave" : "Save", async () =>
                    {
                        bool saveState = _comment.Saved == false;
                        await _redditClient.ToggleSave(_comment, saveState);
                        _comment.Saved = saveState;
                    }),
                    new("Copy Raw", async () => await Clipboard.SetTextAsync(_comment.Body)),
                    new("Copy Permalink", async () => await Clipboard.SetTextAsync(_comment.Permalink))
                );
            }
            else
            {
                await _multiselector.Select(
                    "Select:",
                    new($"{(_comment.SendReplies == true ? "Disable" : "Enable")} Replies", async () =>
                    {
                        bool newReplyState = _comment.SendReplies == false;
                        await _redditClient.ToggleInboxReplies(_comment, newReplyState);
                        _comment.SendReplies = newReplyState;
                    }),
                    new("Delete", async () =>
                    {
                        await _redditClient.Delete(_comment);
                        OnDelete?.Invoke(this, new OnDeleteClickedEventArgs(_comment, this));
                    }),
                    new("Edit", async () =>
                    {
                        ReplyPage replyPage = await AppNavigator.OpenEditPage(_comment);
                        replyPage.OnSubmitted += this.EditPage_OnSubmitted;
                    })
                );
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
                _bottomBar.SetUpvoteState(UpvoteState.None);
            }
            else if (_comment.Likes == UpvoteState.Downvote)
            {
                _comment.Score += 2;
                _commentHeader.SetIndicatorState(UpvoteState.Upvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Upvote);
                _bottomBar.SetUpvoteState(UpvoteState.Upvote);
            }
            else
            {
                _comment.Score++;
                _commentHeader.SetIndicatorState(UpvoteState.Upvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Upvote);
                _bottomBar.SetUpvoteState(UpvoteState.Upvote);
            }
        }

        public async Task Select()
        {
            _commentContainer.BackgroundColor = _applicationStyling.HighlightColor.ToHex();
            _topBar.Display = "flex";
            _bottomBar.Display = "flex";
            _replies.BorderLeft = $"1px solid {_applicationStyling.HighlightColor.ToHex()}";

            if (_comment.Unread == true)
            {
                await _redditClient.MarkRead(_comment, true);
                _comment.Unread = false;
            }
        }

        public Task Unselect()
        {
            _commentContainer.BackgroundColor = null;
            _topBar.Display = "none";
            _bottomBar.Display = "none";
            _replies.BorderLeft = $"1px solid {_applicationStyling.TextColor.ToHex()}";
            return Task.CompletedTask;
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

                    string mimeType = UrlHelper.GetMimeTypeFromUri(new Uri(href));

                    if (mimeType.StartsWith("image/"))
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
            _comment.BodyHtml = e.NewComment.BodyHtml;
            _commentBody.InnerHTML = e.NewComment.BodyHtml;
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
                _bottomBar.SetUpvoteState(UpvoteState.None);
            }
            else if (_comment.Likes == UpvoteState.Upvote)
            {
                _comment.Score -= 2;
                _commentHeader.SetIndicatorState(UpvoteState.Downvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Downvote);
                _bottomBar.SetUpvoteState(UpvoteState.Downvote);
            }
            else
            {
                _comment.Score--;
                _commentHeader.SetIndicatorState(UpvoteState.Downvote);
                _redditClient.SetUpvoteState(_comment, UpvoteState.Downvote);
                _bottomBar.SetUpvoteState(UpvoteState.Downvote);
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

        private async void SelectClick(object? sender, EventArgs e)
        {
            if (SelectionGroup != null)
            {
                await SelectionGroup.Select(this);
            }
        }
    }
}