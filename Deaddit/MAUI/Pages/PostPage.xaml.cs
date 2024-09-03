using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.MAUI.Components;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Services;
using Deaddit.Utils;

namespace Deaddit.MAUI.Pages
{
    public partial class PostPage : ContentPage
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly SelectionGroup _commentSelectionTracker;

        private readonly IConfigurationService _configurationService;

        private readonly IVisitTracker _visitTracker;

        private readonly RedditPost _post;

        private readonly IRedditClient _redditClient;

        public PostPage(RedditPost post, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _commentSelectionTracker = new SelectionGroup();
            _configurationService = configurationService;
            _post = post;
            _visitTracker = visitTracker;
            _blockConfiguration = blockConfiguration;
            _applicationTheme = applicationTheme;
            _redditClient = redditClient;

            BindingContext = new PostPageViewModel(applicationTheme);

            this.InitializeComponent();

            RedditPostComponent redditPostComponent = RedditPostComponent.PostView(post, redditClient, applicationTheme, visitTracker, new SelectionGroup(), _blockConfiguration, _configurationService);

            //After the menu bar which is hardcoded
            mainStack.Children.Insert(0, redditPostComponent);

            DataService.LoadAsync(mainStack, this.LoadDataAsync, _applicationTheme.HighlightColor);
        }

        public void OnBackClicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        public void OnHideClicked(object sender, EventArgs e)
        {
        }

        public void OnMoreOptionsClicked(object sender, EventArgs e)
        {
        }

        public async void OnReplyClicked(object sender, EventArgs e)
        {
            ReplyPage replyPage = new(_post, _redditClient, _applicationTheme, _visitTracker, _blockConfiguration, _configurationService);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
            await Navigation.PushAsync(replyPage);
        }

        public void OnSaveClicked(object sender, EventArgs e)
        {
        }

        public async void OnShareClicked(object sender, EventArgs e)
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = _post.Url,
                Title = _post.Title
            });
        }

        private async Task LoadDataAsync()
        {
            List<CommentReadResponse> response = await _redditClient.Comments(_post);

            foreach (CommentReadResponse responseItem in response)
            {
                if (responseItem.Data is null)
                {
                    continue;
                }

                foreach (RedditCommentMeta comment in responseItem.Data.Children)
                {
                    Ensure.NotNull(comment.Data, "Comment Data");

                    if (comment.Kind is ThingKind.Comment)
                    {
                        if (!_blockConfiguration.BlockRules.IsAllowed(comment.Data))
                        {
                            continue;
                        }

                        RedditCommentComponent commentComponent = RedditCommentComponent.FullView(comment.Data, _redditClient, _applicationTheme, _visitTracker, _commentSelectionTracker, _blockConfiguration, _configurationService);

                        mainStack.Add(commentComponent);

                        if (comment.Data?.Replies?.Data?.Children is not null)
                        {
                            commentComponent.AddChildren(comment.Data.Replies.Data.Children);
                        }
                    }
                }
            }
        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment.Data, "New Comment Data");

            RedditCommentComponent redditCommentComponent = RedditCommentComponent.FullView(e.NewComment.Data, _redditClient, _applicationTheme, _visitTracker, _commentSelectionTracker, _blockConfiguration, _configurationService);

            mainStack.Children.Insert(0, redditCommentComponent);
        }
    }
}