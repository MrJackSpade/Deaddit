using Deaddit.Components.WebComponents;
using Deaddit.Components.WebComponents.Partials.Post;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.Options;
using Deaddit.Core.Utils;
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

        private readonly IDisplayExceptions _displayExceptions;

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

        public PostPage(ApiPost post, ApiComment? focus, IDisplayExceptions displayExceptions, IAggregateUrlHandler urlHandler, IAggregatePostHandler aggregatePostHandler, IAppNavigator appNavigator, IConfigurationService configurationService, IRedditClient redditClient, ApplicationStyling applicationStyling, ApplicationHacks applicationHacks, BlockConfiguration blockConfiguration)
        {
            NavigationPage.SetHasNavigationBar(this, false);

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

            if (!string.IsNullOrWhiteSpace(post.BodyHtml) && post.Body.Length > applicationHacks.MinimumPostBodyLenth)
            {
                postBody = new PostBodyComponent(post, applicationStyling);
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
            moreButton.OnClick += this.OnMoreOptionsClicked;
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

        public virtual async void OnMoreOptionsClicked(object? sender, EventArgs e)
        {
            Dictionary<PostMoreOptions, string?> options = [];

            options.Add(PostMoreOptions.BlockAuthor, $"Block /u/{Post.Author}");
            options.Add(PostMoreOptions.BlockSubreddit, $"Block /r/{Post.SubReddit}");
            options.Add(PostMoreOptions.ViewAuthor, $"View /u/{Post.Author}");
            options.Add(PostMoreOptions.ViewSubreddit, $"View /r/{Post.SubReddit}");

            if (!string.IsNullOrWhiteSpace(Post.Domain))
            {
                options.Add(PostMoreOptions.BlockDomain, $"Block {Post.Domain}");
            }
            else
            {
                options.Add(PostMoreOptions.BlockDomain, null);
            }

            if (!string.IsNullOrWhiteSpace(Post.LinkFlairText))
            {
                options.Add(PostMoreOptions.BlockFlair, $"Block [{Post.LinkFlairText}]");
            }
            else
            {
                options.Add(PostMoreOptions.BlockFlair, null);
            }

            PostMoreOptions? postMoreOptions = await this.DisplayActionSheet("Select:", null, null, options);

            if (postMoreOptions is null)
            {
                return;
            }

            switch (postMoreOptions.Value)
            {
                case PostMoreOptions.ViewAuthor:
                    await AppNavigator.OpenUser(Post.Author);
                    break;

                case PostMoreOptions.BlockFlair:
                    await this.NewBlockRule(new BlockRule()
                    {
                        Flair = Post.LinkFlairText,
                        SubReddit = Post.SubReddit,
                        BlockType = BlockType.Post,
                        RuleName = $"{Post.SubReddit} [{Post.LinkFlairText}]"
                    });
                    break;

                case PostMoreOptions.BlockSubreddit:
                    await this.NewBlockRule(new BlockRule()
                    {
                        SubReddit = Post.SubReddit,
                        BlockType = BlockType.Post,
                        RuleName = $"/r/{Post.SubReddit}"
                    });
                    break;

                case PostMoreOptions.ViewSubreddit:
                    await AppNavigator.OpenSubReddit(Post.SubReddit, ApiPostSort.Hot);
                    break;

                case PostMoreOptions.BlockAuthor:
                    await this.NewBlockRule(new BlockRule()
                    {
                        Author = Post.Author,
                        BlockType = BlockType.Post,
                        RuleName = $"/u/{Post.Author}"
                    });
                    break;

                case PostMoreOptions.BlockDomain:
                    if (!string.IsNullOrWhiteSpace(Post.Domain))
                    {
                        await this.NewBlockRule(new BlockRule()
                        {
                            Domain = Post.Domain,
                            BlockType = BlockType.Post,
                            RuleName = $"({Post.Domain})"
                        });
                    }

                    break;

                default: throw new EnumNotImplementedException(postMoreOptions.Value);
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
                BlockConfiguration.BlockRules.Add(blockRule);

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