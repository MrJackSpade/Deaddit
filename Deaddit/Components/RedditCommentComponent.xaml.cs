using Deaddit.Components.ComponentModels;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Models;
using Deaddit.Models.Json.Response;
using Deaddit.Pages;
using Deaddit.Services;

namespace Deaddit.Components
{
    public partial class RedditCommentComponent : ContentView, ISelectable
    {
        private readonly IAppTheme _appTheme;

        private readonly RedditCommentComponentViewModel _commentViewModel;

        private readonly IMarkDownService _markDownService;

        private readonly IRedditClient _redditClient;

        private readonly ISelectionTracker _selectionTracker;

        private readonly RedditThing _thing;

        private RedditCommentComponent(RedditThing thing, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            _appTheme = appTheme;
            _redditClient = redditClient;
            _thing = thing;
            _markDownService = markDownService;
            _selectionTracker = selectionTracker;

            BindingContext = _commentViewModel = new RedditCommentComponentViewModel(thing, appTheme, markDownService);
            this.InitializeComponent();
        }

        public bool SelectEnabled { get; private set; }

        public static RedditCommentComponent FullView(RedditThing thing, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            RedditCommentComponent toReturn = new(thing, redditClient, appTheme, selectionTracker, markDownService)
            {
                SelectEnabled = true
            };

            return toReturn;
        }

        public static RedditCommentComponent Preview(RedditThing thing, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            RedditCommentComponent toReturn = new(thing, redditClient, appTheme, selectionTracker, markDownService)
            {
                SelectEnabled = false
            };

            return toReturn;
        }

        public void AddChildren(IEnumerable<RedditThing> children)
        {
            foreach (RedditThing child in children)
            {
                RedditCommentComponent childComponent = FullView(child, _redditClient, _appTheme, _selectionTracker, _markDownService);
                childStack.Add(childComponent);
            }
        }

        public void AddChildren(IEnumerable<RedditCommentMeta> children)
        {
            this.AddChildren(children.Where(c => c.Kind == Deaddit.Models.Json.ThingKind.Comment).Select(c => c.Data));
        }

        public void OnDownvoteClicked(object sender, EventArgs e)
        {
            if (_thing.Likes == UpvoteState.Downvote)
            {
                _thing.Likes = UpvoteState.None;
                _commentViewModel.TryAdjustScore(1);
            }
            else
            {
                _thing.Likes = UpvoteState.Downvote;
                _commentViewModel.TryAdjustScore(-1);
            }

            _commentViewModel.SetUpvoteState(_thing.Likes);
            _redditClient.SetUpvoteState(_thing, _thing.Likes);
        }

        public async void OnReplyClicked(object sender, EventArgs e)
        {
            ReplyPage replyPage = new(this._thing, _redditClient, _appTheme, _markDownService);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
            await Navigation.PushAsync(replyPage);
        }

        public void OnUpvoteClicked(object sender, EventArgs e)
        {
            if (_thing.Likes == UpvoteState.Upvote)
            {
                _thing.Likes = UpvoteState.None;
                _commentViewModel.TryAdjustScore(-1);
            }
            else
            {
                _thing.Likes = UpvoteState.Upvote;
                _commentViewModel.TryAdjustScore(1);
            }

            _commentViewModel.SetUpvoteState(_thing.Likes);
            _redditClient.SetUpvoteState(_thing, _thing.Likes);
        }

        void ISelectable.Select()
        {
            topBar.IsVisible = true;
            bottomBar.IsVisible = true;
            commentBody.BackgroundColor = _appTheme.HighlightColor;
        }

        void ISelectable.Unselect()
        {
            topBar.IsVisible = false;
            bottomBar.IsVisible = false;
            commentBody.BackgroundColor = _appTheme.SecondaryColor;
        }

        private async void MarkdownView_OnHyperLinkClicked(object sender, LinkEventArgs e)
        {
            RedditResource resource = UrlHandler.Resolve(e.Url);

            await Navigation.OpenResource(resource, _redditClient, _appTheme, null);
        }

        private void OnDoneClicked(object sender, EventArgs e)
        {
            // Handle Done click
        }

        private void OnHideClicked(object sender, EventArgs e)
        {
            // Handle Hide click
        }

        private void OnMoreClicked(object sender, EventArgs e)
        {
            // Handle More click
        }

        private void OnParentClicked(object sender, EventArgs e)
        {
            // Handle Parent click
        }

        private void OnParentTapped(object sender, TappedEventArgs e)
        {
            _selectionTracker.Toggle(this);
        }

        private void OnShareClicked(object sender, EventArgs e)
        {
        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            RedditCommentComponent redditCommentComponent = RedditCommentComponent.FullView(e.NewComment.Data, _redditClient, _appTheme, _selectionTracker, _markDownService);

            this.childStack.Children.Insert(0, redditCommentComponent);
        }
    }
}