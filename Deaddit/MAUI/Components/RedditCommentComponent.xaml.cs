using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components.ComponentModels;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Extensions;
using Deaddit.MAUI.Pages;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;
using System.Diagnostics.CodeAnalysis;

namespace Deaddit.MAUI.Components
{
    public partial class RedditCommentComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly RedditCommentComponentViewModel _commentViewModel;

        private readonly IConfigurationService _configurationService;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly IVisitTracker _visitTracker;

        private readonly RedditThing _thing;

        private RedditCommentComponent(RedditThing thing, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _applicationTheme = applicationTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _thing = thing;
            _visitTracker = visitTracker;
            _configurationService = configurationService;
            _selectionGroup = selectionTracker;

            BindingContext = _commentViewModel = new RedditCommentComponentViewModel(thing, applicationTheme);
            this.InitializeComponent();
        }

        public bool SelectEnabled { get; private set; }

        public static RedditCommentComponent FullView(RedditThing thing, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditCommentComponent toReturn = new(thing, redditClient, applicationTheme, visitTracker, selectionTracker, blockConfiguration, configurationService)
            {
                SelectEnabled = true
            };

            return toReturn;
        }

        public static RedditCommentComponent Preview(RedditThing thing, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditCommentComponent toReturn = new(thing, redditClient, applicationTheme, visitTracker, selectionTracker, blockConfiguration, configurationService)
            {
                SelectEnabled = false
            };

            return toReturn;
        }

        public void AddChildren(IEnumerable<RedditThing> children)
        {
            foreach (RedditThing child in children)
            {
                if (!_blockConfiguration.BlockRules.IsAllowed(child))
                {
                    continue;
                }

                RedditCommentComponent childComponent = FullView(child, _redditClient, _applicationTheme, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService);
                childStack.Add(childComponent);
            }
        }

        public void AddChildren(IEnumerable<RedditCommentMeta> children)
        {
            this.AddChildren(children.Where(c => c.Kind == ThingKind.Comment).Select(c => c.Data));
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
            ReplyPage replyPage = new(_thing, _redditClient, _applicationTheme, _visitTracker, _blockConfiguration, _configurationService);
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

        void ISelectionGroupItem.Select()
        {
            topBar.IsVisible = true;
            bottomBar.IsVisible = true;
            commentBody.BackgroundColor = _applicationTheme.HighlightColor;
        }

        void ISelectionGroupItem.Unselect()
        {
            topBar.IsVisible = false;
            bottomBar.IsVisible = false;
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

        public async void OnMoreClicked(object sender, EventArgs e)
        {
            string result = await this.DisplayActionSheet("Select", "Cancel", null, "Option 1", "Option 2", "Option 3");
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
            _selectionGroup.Toggle(this);
        }

        private void OnShareClicked(object sender, EventArgs e)
        {
        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment.Data, "New comment data");

            RedditCommentComponent redditCommentComponent = FullView(e.NewComment.Data, _redditClient, _applicationTheme, _visitTracker, _selectionGroup, _blockConfiguration, _configurationService);

            childStack.Children.Insert(0, redditCommentComponent);
        }
    }
}