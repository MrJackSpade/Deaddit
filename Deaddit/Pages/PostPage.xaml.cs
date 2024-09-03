using Deaddit.Components;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Models.Json;
using Deaddit.Models.Json.Response;
using Deaddit.PageModels;
using Deaddit.Services;

namespace Deaddit.Pages
{
    public partial class PostPage : ContentPage
    {
        private readonly IAppTheme _appTheme;

        private readonly ISelectionTracker _commentSelectionTracker;

        private readonly IMarkDownService _markDownService;

        private readonly RedditPost _post;

        private readonly IRedditClient _redditClient;

        private readonly PostPageViewModel _viewModel;

        private readonly IBlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        public PostPage(RedditPost post, IRedditClient redditClient, IAppTheme appTheme, IMarkDownService markDownService, IBlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _commentSelectionTracker = new SelectionTracker();
            _configurationService = configurationService;
            _post = post;
            _blockConfiguration = blockConfiguration;
            _appTheme = appTheme;
            _redditClient = redditClient;
            _markDownService = markDownService;

            BindingContext = _viewModel = new PostPageViewModel(appTheme, post);

            this.InitializeComponent();

            RedditPostComponent redditPostComponent = RedditPostComponent.PostView(post, redditClient, appTheme, new SelectionTracker(), _markDownService, _blockConfiguration, _configurationService);

            //After the menu bar which is hardcoded
            mainStack.Children.Insert(0, redditPostComponent);

            DataService.LoadAsync(mainStack, this.LoadDataAsync, _appTheme.HighlightColor);
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
            ReplyPage replyPage = new(this._post, _redditClient, _appTheme, _markDownService, _blockConfiguration, _configurationService);
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
                    if (comment.Kind is ThingKind.Comment)
                    {
                        if (!_blockConfiguration.BlockRules.IsAllowed(comment.Data))
                        {
                            continue;
                        }

                        RedditCommentComponent commentComponent = RedditCommentComponent.FullView(comment.Data, _redditClient, _appTheme, _commentSelectionTracker, _markDownService, _blockConfiguration, _configurationService);

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
            RedditCommentComponent redditCommentComponent = RedditCommentComponent.FullView(e.NewComment.Data, _redditClient, _appTheme, _commentSelectionTracker, _markDownService, _blockConfiguration, _configurationService);

            this.mainStack.Children.Insert(0, redditCommentComponent);
        }
    }
}