using Deaddit.Components;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.Options;
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
    public partial class PostPage : ContentPage, IHasChildren
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly ApiComment? _commentFocus;

        private readonly IConfigurationService _configurationService;

        private readonly IRedditClient _redditClient;

        public IAppNavigator AppNavigator { get; }

        public BlockConfiguration BlockConfiguration { get; }

        Layout IHasChildren.ChildContainer => mainStack;

        public ApiPost Post { get; }

        public SelectionGroup SelectionGroup { get; }

        public PostPage(ApiPost post, ApiComment? focus, IAppNavigator appNavigator, IConfigurationService configurationService, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, BlockConfiguration blockConfiguration)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _configurationService = configurationService;
            AppNavigator = appNavigator;
            _commentFocus = focus;
            SelectionGroup = new SelectionGroup();
            Post = post;
            BlockConfiguration = blockConfiguration;
            _applicationStyling = applicationTheme;
            _redditClient = redditClient;

            this.InitializeComponent();

            //RedditPostComponent redditPostComponent = AppNavigator.CreatePostComponent(post, false, null);

            BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();

            postBodyBorder.Stroke = _applicationStyling.TertiaryColor.ToMauiColor();
            postBodyBorder.IsVisible = !string.IsNullOrWhiteSpace(post.Body);
            postBodyBorder.BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor();
            postBodyBorder.HorizontalOptions = LayoutOptions.Center;

            postBody.HyperlinkColor = _applicationStyling.HyperlinkColor.ToMauiColor();
            postBody.BlockQuoteBorderColor = _applicationStyling.TextColor.ToMauiColor();
            postBody.TextColor = _applicationStyling.TextColor.ToMauiColor();
            postBody.BlockQuoteBackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
            postBody.BlockQuoteTextColor = _applicationStyling.TextColor.ToMauiColor();
            postBody.MarkdownText = MarkDownHelper.Clean(applicationHacks.CleanBody(post.Body));

            shareButton.TextColor = _applicationStyling.TextColor.ToMauiColor();
            saveButton.TextColor = _applicationStyling.TextColor.ToMauiColor();
            moreButton.TextColor = _applicationStyling.TextColor.ToMauiColor();
            replyButton.TextColor = _applicationStyling.TextColor.ToMauiColor();

            saveButton.Text = Post.Saved == true ? "Unsave" : "Save";

            //mainStack.Children.Insert(0, redditPostComponent);
        }

        public void InitChildContainer()
        {
        }

        public async void MoreCommentsClick(object? sender, IMore e)
        {
            MoreCommentsComponent mcomponent = sender as MoreCommentsComponent;

            await DataService.LoadAsync(mainStack, async () => await this.LoadMoreAsync(Post, e), _applicationStyling.HighlightColor.ToMauiColor());

            mainStack.Children.Remove(mcomponent);
        }

        public virtual void OnBackClicked(object? sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        public virtual async void OnHideClicked(object? sender, EventArgs e)
        {
            await _redditClient.ToggleVisibility(Post, false);
        }

        public virtual async void OnHyperLinkClicked(object? sender, LinkEventArgs e)
        {
            Ensure.NotNullOrWhiteSpace(e.Url);

            PostItems resource = UrlHelper.Resolve(e.Url);

            await Navigation.OpenResource(resource, AppNavigator);
        }

        public virtual void OnImagesClicked(object? sender, EventArgs e)
        {
            foreach (RedditCommentComponent commentComponent in mainStack.Children.OfType<RedditCommentComponent>())
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
                saveButton.Text = "Save";
            }
            else
            {
                await _redditClient.ToggleSave(Post, true);
                Post.Saved = true;
                saveButton.Text = "Unsave";
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
            await DataService.LoadAsync(mainStack, this.LoadDataAsync, _applicationStyling.HighlightColor.ToMauiColor());
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

        private void ReplyPage_OnSubmitted(object? sender, ReplySubmittedEventArgs e)
        {
            Ensure.NotNull(e.NewComment, "New Comment Data");

            RedditCommentComponent redditCommentComponent = AppNavigator.CreateCommentComponent(e.NewComment, Post, SelectionGroup);

            redditCommentComponent.OnDelete += (s, e) => mainStack.Remove(redditCommentComponent);

            mainStack.Children.InsertAfter(postBodyBorder, redditCommentComponent);
        }
    }
}