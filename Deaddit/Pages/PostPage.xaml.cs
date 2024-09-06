using Deaddit.Components;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Extensions;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.MAUI.Components;
using Deaddit.Utils;
using System.Diagnostics;

namespace Deaddit.Pages
{
    public partial class PostPage : ContentPage
    {
        private readonly ApplicationHacks _applicationHacks;

        private readonly ApplicationStyling _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly ApiComment? _commentFocus;

        private readonly SelectionGroup _commentSelectionGroup;

        private readonly IConfigurationService _configurationService;

        private readonly ApiPost _post;

        private readonly IRedditClient _redditClient;

        private readonly IVisitTracker _visitTracker;

        public PostPage(ApiPost post,
                        IRedditClient redditClient,
                        ApplicationStyling applicationTheme,
                        ApplicationHacks applicationHacks,
                        IVisitTracker visitTracker,
                        BlockConfiguration blockConfiguration,
                        IConfigurationService configurationService) : this(post,
                                                                           null,
                                                                           redditClient,
                                                                           applicationTheme,
                                                                           applicationHacks,
                                                                           visitTracker,
                                                                           blockConfiguration,
                                                                           configurationService)
        {
        }

        public PostPage(ApiPost post, ApiComment? focus, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, IVisitTracker visitTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _commentFocus = focus;
            _commentSelectionGroup = new SelectionGroup();
            _configurationService = configurationService;
            _post = post;
            _visitTracker = visitTracker;
            _applicationHacks = applicationHacks;
            _blockConfiguration = blockConfiguration;
            _applicationTheme = applicationTheme;
            _redditClient = redditClient;

            this.InitializeComponent();
            RedditPostComponent redditPostComponent = RedditPostComponent.PostView(post, redditClient, applicationTheme, applicationHacks, visitTracker, new SelectionGroup(), _blockConfiguration, _configurationService);

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

            backButton.TextColor = _applicationTheme.TextColor.ToMauiColor();
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

            PostTarget resource = UrlHandler.Resolve(e.Url);

            await Navigation.OpenResource(resource, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
        }

        public void OnMoreOptionsClicked(object? sender, EventArgs e)
        {
        }

        public async void OnReplyClicked(object? sender, EventArgs e)
        {
            ReplyPage replyPage = new(_post, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _blockConfiguration, _configurationService);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
            await Navigation.PushAsync(replyPage);
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

        private void AddChildren(IEnumerable<ApiCommentMeta> children)
        {
            foreach (ApiCommentMeta child in children)
            {
                if (!_blockConfiguration.BlockRules.IsAllowed(child.Data))
                {
                    continue;
                }

                if ((child.IsDeleted() || child.IsRemoved()) && !child.HasChildren())
                {
                    continue;
                }

                ContentView? childComponent = null;

                switch (child.Kind)
                {
                    case ThingKind.Comment:
                        RedditCommentComponent ccomponent = RedditCommentComponent.FullView(child.Data, _post, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _commentSelectionGroup, _blockConfiguration, _configurationService);
                        ccomponent.AddChildren(child.GetReplies());
                        ccomponent.OnDelete += this.OnCommentDelete;
                        //outer most comment padded only.
                        ccomponent.Margin = new Thickness(0, 0, 10, 0);
                        childComponent = ccomponent;
                        break;

                    case ThingKind.More:
                        MoreCommentsComponent mcomponent = new(child.Data, _applicationTheme);
                        mcomponent.OnClick += this.MoreCommentsClick;
                        childComponent = mcomponent;
                        break;

                    default:
                        throw new EnumNotImplementedException(child.Kind);
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

            List<ApiCommentMeta> response = await _redditClient.Comments(_post, _commentFocus).ToList();

            this.AddChildren(response);

            sw.Stop();

            Debug.WriteLine("LoadDataAsync: " + sw.ElapsedMilliseconds + "ms");
        }

        private async Task LoadMoreAsync(ApiPost post, ApiComment more)
        {
            List<ApiCommentMeta> response = await _redditClient.MoreComments(post, more).ToList();

            this.AddChildren(response);
        }

        private async void MoreCommentsClick(object? sender, ApiComment e)
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
            Ensure.NotNull(e.NewComment.Data, "New Comment Data");

            RedditCommentComponent redditCommentComponent = RedditCommentComponent.FullView(e.NewComment.Data, _post, _redditClient, _applicationTheme, _applicationHacks, _visitTracker, _commentSelectionGroup, _blockConfiguration, _configurationService);

            redditCommentComponent.OnDelete += this.OnCommentDelete;

            mainStack.Children.InsertAfter(postBodyBorder, redditCommentComponent);
        }
    }
}