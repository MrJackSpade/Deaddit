using Deaddit.Components;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Extensions;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components;
using Deaddit.Utils;
using System.Diagnostics;

namespace Deaddit.Pages
{
    public partial class PostPage : ContentPage
    {
        private readonly ApplicationStyling _applicationTheme;

        private readonly IAppNavigator _appNavigator;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly ApiComment? _commentFocus;

        private readonly SelectionGroup _commentSelectionGroup;

        private readonly ApiPost _post;

        private readonly IRedditClient _redditClient;

        public PostPage(ApiPost post, ApiComment? focus, IAppNavigator appNavigator, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, BlockConfiguration blockConfiguration)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _appNavigator = appNavigator;
            _commentFocus = focus;
            _commentSelectionGroup = new SelectionGroup();
            _post = post;
            _blockConfiguration = blockConfiguration;
            _applicationTheme = applicationTheme;
            _redditClient = redditClient;

            this.InitializeComponent();

            RedditPostComponent redditPostComponent = _appNavigator.CreatePostComponent(post, null);

            BackgroundColor = _applicationTheme.SecondaryColor.ToMauiColor();

            postBodyBorder.Stroke = _applicationTheme.TertiaryColor.ToMauiColor();
            postBodyBorder.IsVisible = !string.IsNullOrWhiteSpace(post.Body);
            postBodyBorder.BackgroundColor = _applicationTheme.PrimaryColor.ToMauiColor();
            postBodyBorder.HorizontalOptions = LayoutOptions.Center;

            postBody.HyperlinkColor = _applicationTheme.HyperlinkColor.ToMauiColor();
            postBody.BlockQuoteBorderColor = _applicationTheme.TextColor.ToMauiColor();
            postBody.TextColor = _applicationTheme.TextColor.ToMauiColor();
            postBody.BlockQuoteBackgroundColor = _applicationTheme.SecondaryColor.ToMauiColor();
            postBody.BlockQuoteTextColor = _applicationTheme.TextColor.ToMauiColor();
            postBody.MarkdownText = MarkDownHelper.Clean(applicationHacks.CleanBody(post.Body));

            shareButton.TextColor = _applicationTheme.TextColor.ToMauiColor();
            saveButton.TextColor = _applicationTheme.TextColor.ToMauiColor();
            moreButton.TextColor = _applicationTheme.TextColor.ToMauiColor();
            replyButton.TextColor = _applicationTheme.TextColor.ToMauiColor();

            mainStack.Children.Insert(0, redditPostComponent);
        }

        public void OnBackClicked(object? sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        public async void OnHideClicked(object? sender, EventArgs e)
        {
            await _redditClient.ToggleVisibility(_post, false);
        }

        public async void OnHyperLinkClicked(object? sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);

            PostItems resource = RedditPostExtensions.Resolve(e.Url);

            await Navigation.OpenResource(resource, _appNavigator);
        }

        public void OnMoreOptionsClicked(object? sender, EventArgs e)
        {
        }

        public async void OnReplyClicked(object? sender, EventArgs e)
        {
            ReplyPage replyPage = await _appNavigator.OpenReplyPage(_post);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
        }

        public void OnSaveClicked(object? sender, EventArgs e)
        {
        }

        public async void OnShareClicked(object? sender, EventArgs e)
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = _post.Url,
                Title = _post.Title
            });
        }

        public async Task TryLoad()
        {
            await DataService.LoadAsync(mainStack, this.LoadDataAsync, _applicationTheme.HighlightColor.ToMauiColor());
        }

        private void AddChildren(IEnumerable<ApiThing> children)
        {
            foreach (ApiThing child in children)
            {
                if (!_blockConfiguration.BlockRules.IsAllowed(child))
                {
                    continue;
                }

                if (child.IsDeleted() || child.IsRemoved())
                {
                    continue;
                }

                ContentView? childComponent = null;

                if (child is ApiComment comment)
                {
                    RedditCommentComponent ccomponent = _appNavigator.CreateCommentComponent(comment, _post, _commentSelectionGroup);
                    ccomponent.AddChildren(comment.GetReplies());
                    ccomponent.OnDelete += this.OnCommentDelete;
                    //outer most comment padded only.
                    ccomponent.Margin = new Thickness(0, 0, 10, 0);
                    childComponent = ccomponent;
                }
                else if (child is ApiMore more)
                {
                    MoreCommentsComponent mcomponent = _appNavigator.CreateMoreCommentsComponent(more);
                    mcomponent.OnClick += this.MoreCommentsClick;
                    childComponent = mcomponent;
                }
                else
                {
                    throw new NotImplementedException();
                }

                try
                {
                    mainStack.Children.Add(childComponent);
                }
                catch (MissingMethodException mme)
                {
                    //More android weirdness?
                    Debug.WriteLine(mme.Message);
                }
            }
        }

        private async Task LoadDataAsync()
        {
            Stopwatch sw = new();

            sw.Start();

            List<ApiThing> response = await _redditClient.Comments(_post, _commentFocus).ToList();

            this.AddChildren(response);

            sw.Stop();

            Debug.WriteLine("LoadDataAsync: " + sw.ElapsedMilliseconds + "ms");
        }

        private async Task LoadMoreAsync(ApiPost post, ApiMore more)
        {
            List<ApiThing> response = await _redditClient.MoreComments(post, more).ToList();

            this.AddChildren(response);
        }

        private async void MoreCommentsClick(object? sender, ApiMore e)
        {
            MoreCommentsComponent mcomponent = sender as MoreCommentsComponent;

            await DataService.LoadAsync(mainStack, async () => await this.LoadMoreAsync(_post, e), _applicationTheme.HighlightColor.ToMauiColor());

            mainStack.Children.Remove(mcomponent);
        }

        private void OnCommentDelete(object? sender, OnDeleteClickedEventArgs e)
        {
            mainStack.Children.Remove(e.Component);
        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment, "New Comment Data");

            RedditCommentComponent redditCommentComponent = _appNavigator.CreateCommentComponent(e.NewComment, _post, _commentSelectionGroup);

            redditCommentComponent.OnDelete += this.OnCommentDelete;

            mainStack.Children.InsertAfter(postBodyBorder, redditCommentComponent);
        }
    }
}