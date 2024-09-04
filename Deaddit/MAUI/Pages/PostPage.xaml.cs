using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Exceptions;
using Deaddit.Extensions;
using Deaddit.MAUI.Components;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Pages.Models;
using Deaddit.Reddit.Extensions;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Services;
using Deaddit.Utils;
using Deaddit.Utils.Extensions;

namespace Deaddit.MAUI.Pages
{
    public partial class PostPage : ContentPage
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly SelectionGroup _commentSelectionGroup;

        private readonly IConfigurationService _configurationService;

        private readonly IVisitTracker _visitTracker;

        private readonly RedditPost _post;

        private readonly IRedditClient _redditClient;

        public PostPage(RedditPost post, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _commentSelectionGroup = new SelectionGroup();
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

            DataService.LoadAsync(mainStack, async () => this.LoadDataAsync(post), _applicationTheme.HighlightColor);
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

        private void AddChildren(IEnumerable<RedditCommentMeta> children)
        {
            foreach (RedditCommentMeta child in children)
            {
                if (child?.Data is null)
                {
                    continue;
                }

                if (!_blockConfiguration.BlockRules.IsAllowed(child.Data))
                {
                    continue;
                }

                ContentView childComponent = null;

                switch (child.Kind)
                {
                    case ThingKind.Comment:
                        RedditCommentComponent ccomponent = RedditCommentComponent.FullView(child.Data, _post, _redditClient, _applicationTheme, _visitTracker, _commentSelectionGroup, _blockConfiguration, _configurationService);
                        ccomponent.AddChildren(child.GetReplies());
                        childComponent = ccomponent;
                        break;
                    case ThingKind.More:
                        MoreCommentsComponent mcomponent = new(child.Data, _applicationTheme);
                        mcomponent.OnClick += this.MoreCommentsClick;
                        childComponent = mcomponent;
                        break;
                    default:
                        throw new UnhandledEnumException(child.Kind);
                }

                mainStack.Children.Add(childComponent);
            }
        }

        private async Task LoadDataAsync(RedditPost post)
        {
            List<RedditCommentMeta> response = await _redditClient.Comments(post, null).ToList();

            this.AddChildren(response);
        }

        private async Task LoadMoreAsync(RedditPost post, RedditComment more)
        {
            List<RedditCommentMeta> response = await _redditClient.MoreComments(post, more).ToList();

            this.AddChildren(response);
        }

        private async void MoreCommentsClick(object? sender, RedditComment e)
        {
            MoreCommentsComponent mcomponent = sender as MoreCommentsComponent;

            await DataService.LoadAsync(mainStack, async () => await this.LoadMoreAsync(_post, e), _applicationTheme.HighlightColor);

            mainStack.Children.Remove(mcomponent);

        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment.Data, "New Comment Data");

            RedditCommentComponent redditCommentComponent = RedditCommentComponent.FullView(e.NewComment.Data, _post, _redditClient, _applicationTheme, _visitTracker, _commentSelectionGroup, _blockConfiguration, _configurationService);

            mainStack.Children.Insert(0, redditCommentComponent);
        }
    }
}