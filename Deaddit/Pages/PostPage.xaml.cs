using Deaddit.Components.WebComponents;
using Deaddit.Components.WebComponents.Partials.Post;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Reddit.Api.Interfaces;
using Reddit.Api.Models;
using Reddit.Api.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Blocking;
using Deaddit.Core.Utils.MultiSelect;
using Deaddit.Core.Utils.Validation;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Utils;
using Maui.WebComponents.Components;
using Maui.WebComponents.Extensions;
using System.Diagnostics;

namespace Deaddit.Pages
{
    public partial class PostPage : ContentPage, IHasChildren
    {
        private readonly IAggregatePostHandler _aggregatePostHandler;

        private readonly ApplicationStyling _applicationStyling;

        private readonly ApiComment? _commentFocus;

        private readonly IConfigurationService _configurationService;

        private readonly IDisplayMessages _displayExceptions;

        private readonly MultiSelector _multiselector;

        private readonly IRedditClient _redditClient;

        private readonly IAggregateUrlHandler _urlHandler;

        private readonly DivComponent commentContainer;

        private readonly ButtonComponent loadImageButton;

        private readonly ButtonComponent moreButton;

        private readonly DivComponent postBody;

        private readonly ButtonComponent replyButton;

        private readonly ButtonComponent saveButton;

        private readonly ButtonComponent shareButton;

        public IAppNavigator AppNavigator { get; }

        public BlockConfiguration BlockConfiguration { get; }

        WebComponent IHasChildren.ChildContainer => commentContainer;

        public ApiPost Post { get; }

        public SelectionGroup SelectionGroup { get; }

        public PostPage(ApiPost post, ApiComment? focus, ISelectBoxDisplay selectBoxDisplay, IDisplayMessages displayExceptions, IAggregateUrlHandler urlHandler, IAggregatePostHandler aggregatePostHandler, IAppNavigator appNavigator, IConfigurationService configurationService, IRedditClient redditClient, ApplicationStyling applicationStyling, ApplicationHacks applicationHacks, BlockConfiguration blockConfiguration)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _multiselector = new MultiSelector(selectBoxDisplay);
            _urlHandler = urlHandler;
            _aggregatePostHandler = aggregatePostHandler;
            _displayExceptions = displayExceptions;
            _configurationService = configurationService;
            AppNavigator = appNavigator;
            _commentFocus = focus;
            SelectionGroup = new SelectionGroup();
            Post = post;
            BlockConfiguration = blockConfiguration;
            _applicationStyling = applicationStyling;
            _redditClient = redditClient;

            this.InitializeComponent();

            webElement.SetColors(applicationStyling);
            webElement.OnJavascriptError += this.WebElement_OnJavascriptError;

            webElement.ClickUrl += this.WebElement_ClickUrl;

            commentContainer = new DivComponent()
            {
                PaddingRight = "5px",
                BoxSizing = "border-box"
            };

            RedditPostWebComponent redditPostComponent = AppNavigator.CreatePostWebComponent(post, PostState.None, null);

            webElement.AddChild(redditPostComponent);

            if (!string.IsNullOrWhiteSpace(post.Body))
            {           
                //Include hidden characters for removal here 
                string postBodyVisible = new(post.Body.Trim().Where(c => c != (char)8204).ToArray());

                if (postBodyVisible.Length > applicationHacks.MinimumPostBodyLenth)
                {
                    postBody = new PostBodyComponent(post, applicationStyling);
                }
            }

            DivComponent actionButtons = new()
            {
                FlexDirection = "row",
                Width = "100%",
                Display = "flex",
                BackgroundColor = applicationStyling.SecondaryColor.ToHex(),
            };

            shareButton = this.ActionButton("Share");
            saveButton = this.ActionButton("Save");
            moreButton = this.ActionButton("...");
            loadImageButton = this.ActionButton("🖼");
            replyButton = this.ActionButton("Reply");

            shareButton.OnClick += this.OnShareClicked;
            saveButton.OnClick += this.OnSaveClicked;
            moreButton.OnClick += this.OnMoreClicked;
            replyButton.OnClick += this.OnReplyClicked;
            loadImageButton.OnClick += this.OnLoadImageClicked;

            saveButton.InnerText = Post.Saved == true ? "Unsave" : "Save";

            actionButtons.Children.Add(shareButton);
            actionButtons.Children.Add(saveButton);
            actionButtons.Children.Add(moreButton);
            actionButtons.Children.Add(loadImageButton);
            actionButtons.Children.Add(replyButton);

            webElement.AddChild(actionButtons);

            if (postBody is not null)
            {
                webElement.AddChild(postBody);
            }

            webElement.AddChild(commentContainer);
        }

        public void InitChildContainer()
        {
        }

        public async void MoreCommentsClick(object? sender, IMore e)
        {
            MoreCommentsWebComponent mcomponent = sender as MoreCommentsWebComponent;

            await webElement.RemoveChild(mcomponent);

            await DataService.LoadAsync(mcomponent, async () => await this.LoadMoreAsync(Post, e), _applicationStyling.HighlightColor.ToHex());
        }

        public virtual void OnBackClicked(object? sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        public virtual async void OnHideClicked(object? sender, EventArgs e)
        {
            await _redditClient.ToggleVisibility(Post, false);
        }

        public virtual void OnImagesClicked(object? sender, EventArgs e)
        {
            foreach (RedditCommentWebComponent commentComponent in commentContainer.Children.OfType<RedditCommentWebComponent>())
            {
                commentComponent.LoadImages(true);
            }
        }

        public virtual async void OnReplyClicked(object? sender, EventArgs e)
        {
            ReplyPage replyPage = await AppNavigator.OpenReplyPage(Post);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
        }

        public virtual async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (Post.Saved == true)
            {
                await _redditClient.ToggleSave(Post, false);
                Post.Saved = false;
                saveButton.InnerText = "Save";
            }
            else
            {
                await _redditClient.ToggleSave(Post, true);
                Post.Saved = true;
                saveButton.InnerText = "Unsave";
            }
        }

        public virtual async void OnShareClicked(object? sender, EventArgs e)
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = Post.Url,
                Title = Post.Title
            });
        }

        public async Task TryLoad()
        {
            await DataService.LoadAsync(commentContainer, this.LoadDataAsync, _applicationStyling.HighlightColor.ToHex());
        }

        private ButtonComponent ActionButton(string text)
        {
            return new ButtonComponent
            {
                InnerText = text,
                FontSize = $"{_applicationStyling.TitleFontSize}px",
                Color = _applicationStyling.TextColor.ToHex(),
                BackgroundColor = _applicationStyling.SecondaryColor.ToHex(),
                Padding = "10px",
                FlexGrow = "1",
                Border = "0",
            };
        }

        private void BlockRuleOnSave(object? sender, Deaddit.EventArguments.ObjectEditorSaveEventArgs e)
        {
            if (e.Saved is BlockRule blockRule)
            {
                BlockConfiguration.BlackList.Rules.Add(blockRule);

                _configurationService.Write(BlockConfiguration);
            }
        }

        private async Task LoadDataAsync()
        {
            Stopwatch sw = new();

            sw.Start();

            List<ApiThing> response = await _redditClient.Comments(Post, _commentFocus);

            this.AddChildren(response);

            sw.Stop();

            Debug.WriteLine("LoadDataAsync: " + sw.ElapsedMilliseconds + "ms");
        }

        private async Task LoadMoreAsync(ApiPost post, IMore more)
        {
            List<ApiThing> response = await _redditClient.MoreComments(post, more);

            this.AddChildren(response, true);
        }

        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = await AppNavigator.OpenObjectEditor(blockRule);

            objectEditorPage.OnSave += this.BlockRuleOnSave;
        }

        private void OnLoadImageClicked(object? sender, EventArgs e)
        {
            foreach (RedditCommentWebComponent commentComponent in commentContainer.Children.OfType<RedditCommentWebComponent>())
            {
                commentComponent.LoadImages(true);
            }
        }

        private async Task OnMoreBlockClicked()
        {
            await _multiselector.Select(
            "Block:",
            new($"/u/{Post.Author}", async () => await this.NewBlockRule(BlockRuleHelper.FromAuthor(Post))),
            new($"/r/{Post.SubRedditName}", async () => await this.NewBlockRule(BlockRuleHelper.FromSubReddit(Post))),
            new(Post.Domain, async () => await this.NewBlockRule(BlockRuleHelper.FromDomain(Post))),
            new(Post.LinkFlairText, async () => await this.NewBlockRule(BlockRuleHelper.FromFlair(Post))));
        }

        private async void OnMoreClicked(object? sender, EventArgs e)
        {
            await _multiselector.Select(
            "Select:",
            new($"Block...", this.OnMoreBlockClicked),
            new($"View...", this.OnMoreViewClicked),
            new($"Share...", this.OnMoreShareClicked));
        }

        private async Task OnMoreShareClicked()
        {
            await _multiselector.Select(
                "Share:",
                new(null, null),
                new($"Comments", async () => await this.NewBlockRule(BlockRuleHelper.FromAuthor(Post))));
        }

        private async Task OnMoreViewClicked()
        {
            await _multiselector.Select(
            "View:",
            new($"/u/{Post.Author}", async () => await AppNavigator.OpenUser(Post.Author)),
            new($"/r/{Post.SubRedditName}", async () => await AppNavigator.OpenSubReddit(Post.SubRedditName, ApiPostSort.Hot)));
        }

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment, "New Comment Data");

            RedditCommentWebComponent redditCommentComponent = AppNavigator.CreateCommentWebComponent(e.NewComment, Post, SelectionGroup);

            redditCommentComponent.OnDelete += (s, e) => commentContainer.Children.Remove(redditCommentComponent);

            commentContainer.Children.Insert(0, redditCommentComponent);
        }

        private async void WebElement_ClickUrl(object? sender, string e)
        {
            if (!_urlHandler.CanLaunch(e, _aggregatePostHandler))
            {
                await this.DisplayAlert("Alert", $"Can not handle url {e}", "OK");
                return;
            }

            await _urlHandler.Launch(e, _aggregatePostHandler);
        }

        private void WebElement_OnJavascriptError(object? sender, Exception e)
        {
            _displayExceptions.DisplayException(e);
        }
    }
}