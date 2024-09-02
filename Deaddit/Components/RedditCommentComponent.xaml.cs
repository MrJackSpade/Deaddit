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

        private readonly IBlockConfiguration _blockConfiguration;

        private readonly RedditThing _thing;

        private RedditCommentComponent(RedditThing thing, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService, IBlockConfiguration blockConfiguration)
        {
            _appTheme = appTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _thing = thing;
            _markDownService = markDownService;
            _selectionTracker = selectionTracker;

            BindingContext = _commentViewModel = new RedditCommentComponentViewModel(thing, appTheme, markDownService);
            this.InitializeComponent();
        }

        public bool SelectEnabled { get; private set; }

        public static RedditCommentComponent FullView(RedditThing thing, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService, IBlockConfiguration blockConfiguration)
        {
            RedditCommentComponent toReturn = new(thing, redditClient, appTheme, selectionTracker, markDownService, blockConfiguration)
            {
                SelectEnabled = true
            };

            return toReturn;
        }

        public static RedditCommentComponent Preview(RedditThing thing, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService, IBlockConfiguration blockConfiguration)
        {
            RedditCommentComponent toReturn = new(thing, redditClient, appTheme, selectionTracker, markDownService, blockConfiguration)
            {
                SelectEnabled = false
            };

            return toReturn;
        }

        public void AddChildren(IEnumerable<RedditThing> children)
        {
            foreach (RedditThing child in children)
            {
                if (_blockConfiguration.BlockRules.IsBlocked(child))
                {
                    continue;
                }

                RedditCommentComponent childComponent = FullView(child, _redditClient, _appTheme, _selectionTracker, _markDownService, _blockConfiguration);
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
            ReplyPage replyPage = new(this._thing, _redditClient, _appTheme, _markDownService, _blockConfiguration);
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

            await Navigation.OpenResource(resource, _redditClient, _appTheme, null, _blockConfiguration);
        }

        private void OnDoneClicked(object sender, EventArgs e)
        {
            // Handle Done click
        }

        private void OnHideClicked(object sender, EventArgs e)
        {
            // Handle Hide click
        }

        private async void OnMoreClicked(object sender, EventArgs e)
        {
            SelectPage page = new("Option 1", "Option 2", "Option 3");
            page.OnSelect += this.OnMoreSelect;
            await Navigation.PushAsync(page);
        }

        private void OnMoreSelect(object? sender, string e)
        {

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
            RedditCommentComponent redditCommentComponent = FullView(e.NewComment.Data, _redditClient, _appTheme, _selectionTracker, _markDownService, _blockConfiguration);

            this.childStack.Children.Insert(0, redditCommentComponent);
        }
    }
}