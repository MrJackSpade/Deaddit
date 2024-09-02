using Deaddit.Components.ComponentModels;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Models;
using Deaddit.Models.Json.Response;
using Deaddit.Pages;

namespace Deaddit.Components
{
    public partial class RedditPostComponent : ContentView, ISelectable
    {
        private readonly IAppTheme _appTheme;

        private readonly IMarkDownService _markDownService;

        private readonly RedditPost _post;

        private readonly RedditPostComponentViewModel _postViewModel;

        private readonly IRedditClient _redditClient;

        private readonly ISelectionTracker _selectionTracker;

        private RedditPostComponent(RedditPost post, bool postBodyIsVisible, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            _appTheme = appTheme;
            _redditClient = redditClient;
            _selectionTracker = selectionTracker;
            _markDownService = markDownService;
            _post = post;

            BindingContext = _postViewModel = new RedditPostComponentViewModel(post, postBodyIsVisible, appTheme, _markDownService);
            this.InitializeComponent();
        }

        public bool Selected { get; private set; }

        public bool SelectEnabled { get; private set; }

        public static RedditPostComponent ListView(RedditPost post, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            RedditPostComponent toReturn = new(post, false, redditClient, appTheme, selectionTracker, markDownService);
            toReturn.SelectEnabled = true;
            return toReturn;
        }

        public static RedditPostComponent PostView(RedditPost post, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            RedditPostComponent toReturn = new(post, !string.IsNullOrWhiteSpace(post.Body), redditClient, appTheme, selectionTracker, markDownService);
            toReturn.timeUser.IsVisible = true;
            toReturn.SelectEnabled = false;
            return toReturn;
        }

        public async void OnCommentsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PostPage(_post, _redditClient, _appTheme, _markDownService));
        }

        public void OnDownvoteClicked(object sender, EventArgs e)
        {
            if (_post.Likes == UpvoteState.Downvote)
            {
                _post.Likes = UpvoteState.None;
                _postViewModel.TryAdjustScore(1);
            }
            else
            {
                _post.Likes = UpvoteState.Downvote;
                _postViewModel.TryAdjustScore(-1);
            }

            _postViewModel.SetUpvoteState(_post.Likes);
            _redditClient.SetUpvoteState(_post, _post.Likes);
        }

        public void OnHideClicked(object sender, EventArgs e)
        {
            // Handle the Hide button click
        }

        public void OnImageClicked(object sender, EventArgs e)
        {
        }

        public void OnMoreOptionsClicked(object sender, EventArgs e)
        {
            // Handle the More Options button click
        }

        public void OnSaveClicked(object sender, EventArgs e)
        {
            // Handle the Save button click
        }

        public void OnShareClicked(object sender, EventArgs e)
        {
            // Handle the Share button click
        }

        public async void OnThumbnailImageClicked(object sender, EventArgs e)
        {
            await Navigation.OpenPost(_post, _redditClient, _appTheme, _markDownService);
        }

        public void OnUpvoteClicked(object sender, EventArgs e)
        {
            if (_post.Likes == UpvoteState.Upvote)
            {
                _post.Likes = UpvoteState.None;
                _postViewModel.TryAdjustScore(-1);
            }
            else
            {
                _post.Likes = UpvoteState.Upvote;
                _postViewModel.TryAdjustScore(1);
            }

            _postViewModel.SetUpvoteState(_post.Likes);
            _redditClient.SetUpvoteState(_post, _post.Likes);
        }

        void ISelectable.Select()
        {
            Selected = true;
            BackgroundColor = _appTheme.HighlightColor;
            mainGrid.BackgroundColor = _appTheme.HighlightColor;
            timeUser.IsVisible = true;
            actionButtonsStack.IsVisible = true;
        }

        void ISelectable.Unselect()
        {
            Selected = false;
            BackgroundColor = _appTheme.SecondaryColor;
            mainGrid.BackgroundColor = _appTheme.SecondaryColor;
            timeUser.IsVisible = false;
            actionButtonsStack.IsVisible = false;
        }

        private void OnParentTapped(object sender, TappedEventArgs e)
        {
            _selectionTracker.Toggle(this);
        }
    }
}