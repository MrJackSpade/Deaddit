using Deaddit.Components;
using Deaddit.Core.Configurations.Interfaces;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Exceptions;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Extensions;
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
    public partial class PostPage : ContentPage
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly ApiComment? _commentFocus;

        private readonly SelectionGroup _commentSelectionGroup;

        private readonly IConfigurationService _configurationService;

        private readonly ApiPost _post;

        private readonly IRedditClient _redditClient;

        public PostPage(ApiPost post, ApiComment? focus, IAppNavigator appNavigator, IConfigurationService configurationService, IRedditClient redditClient, ApplicationStyling applicationTheme, ApplicationHacks applicationHacks, BlockConfiguration blockConfiguration)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            _configurationService = configurationService;
            _appNavigator = appNavigator;
            _commentFocus = focus;
            _commentSelectionGroup = new SelectionGroup();
            _post = post;
            _blockConfiguration = blockConfiguration;
            _applicationStyling = applicationTheme;
            _redditClient = redditClient;

            this.InitializeComponent();

            RedditPostComponent redditPostComponent = _appNavigator.CreatePostComponent(post, null);

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

            saveButton.Text = _post.Saved == true ? "Unsave" : "Save";

            mainStack.Children.Insert(0, redditPostComponent);
        }

        public void OnImagesClicked(object? sender, EventArgs e)
        {
            foreach (RedditCommentComponent commentComponent in mainStack.Children.OfType<RedditCommentComponent>())
            {
                commentComponent.LoadImages(true);
            }
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

            PostItems resource = UrlHelper.Resolve(e.Url);

            await Navigation.OpenResource(resource, _appNavigator);
        }

        private void BlockRuleOnSave(object? sender, Deaddit.EventArguments.ObjectEditorSaveEventArgs e)
        {
            if (e.Saved is BlockRule blockRule)
            {
                _blockConfiguration.BlockRules.Add(blockRule);

                _configurationService.Write(_blockConfiguration);
            }
        }
        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = await _appNavigator.OpenObjectEditor(blockRule);

            objectEditorPage.OnSave += this.BlockRuleOnSave;
        }

        public async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (_post.Saved == true)
            {
                await _redditClient.ToggleSave(_post, false);
                _post.Saved = false;
                saveButton.Text = "Save";
            }
            else
            {
                await _redditClient.ToggleSave(_post, true);
                _post.Saved = true;
                saveButton.Text = "Unsave";
            }
        }

        public async void OnMoreOptionsClicked(object? sender, EventArgs e)
        {

            Dictionary<PostMoreOptions, string?> options = [];

            options.Add(PostMoreOptions.BlockAuthor, $"Block /u/{_post.Author}");
            options.Add(PostMoreOptions.BlockSubreddit, $"Block /r/{_post.SubReddit}");
            options.Add(PostMoreOptions.ViewAuthor, $"View /u/{_post.Author}");
            options.Add(PostMoreOptions.ViewSubreddit, $"View /r/{_post.SubReddit}");

            if (!string.IsNullOrWhiteSpace(_post.Domain))
            {
                options.Add(PostMoreOptions.BlockDomain, $"Block {_post.Domain}");
            }
            else
            {
                options.Add(PostMoreOptions.BlockDomain, null);
            }

            if (!string.IsNullOrWhiteSpace(_post.LinkFlairText))
            {
                options.Add(PostMoreOptions.BlockFlair, $"Block [{_post.LinkFlairText}]");
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
                    await _appNavigator.OpenUser(_post.Author);
                    break;

                case PostMoreOptions.BlockFlair:
                    await this.NewBlockRule(new BlockRule()
                    {
                        Flair = _post.LinkFlairText,
                        SubReddit = _post.SubReddit,
                        BlockType = BlockType.Post,
                        RuleName = $"{_post.SubReddit} [{_post.LinkFlairText}]"
                    });
                    break;

                case PostMoreOptions.BlockSubreddit:
                    await this.NewBlockRule(new BlockRule()
                    {
                        SubReddit = _post.SubReddit,
                        BlockType = BlockType.Post,
                        RuleName = $"/r/{_post.SubReddit}"
                    });
                    break;

                case PostMoreOptions.ViewSubreddit:
                    await _appNavigator.OpenSubReddit(_post.SubReddit, ApiPostSort.Hot);
                    break;

                case PostMoreOptions.BlockAuthor:
                    await this.NewBlockRule(new BlockRule()
                    {
                        Author = _post.Author,
                        BlockType = BlockType.Post,
                        RuleName = $"/u/{_post.Author}"
                    });
                    break;

                case PostMoreOptions.BlockDomain:
                    if (!string.IsNullOrWhiteSpace(_post.Domain))
                    {
                        await this.NewBlockRule(new BlockRule()
                        {
                            Domain = _post.Domain,
                            BlockType = BlockType.Post,
                            RuleName = $"({_post.Domain})"
                        });
                    }

                    break;

                default: throw new EnumNotImplementedException(postMoreOptions.Value);
            }
        }

        public async void OnReplyClicked(object? sender, EventArgs e)
        {
            ReplyPage replyPage = await _appNavigator.OpenReplyPage(_post);
            replyPage.OnSubmitted += this.ReplyPage_OnSubmitted;
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
            await DataService.LoadAsync(mainStack, this.LoadDataAsync, _applicationStyling.HighlightColor.ToMauiColor());
        }

        private void AddChildren(IEnumerable<ApiThing> children)
        {
            foreach (ApiThing child in children)
            {
                if (!_blockConfiguration.IsAllowed(child))
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

        private async Task LoadMoreAsync(ApiPost post, IMore more)
        {
            List<ApiThing> response = await _redditClient.MoreComments(post, more).ToList();

            this.AddChildren(response);
        }

        private async void MoreCommentsClick(object? sender, IMore e)
        {
            MoreCommentsComponent mcomponent = sender as MoreCommentsComponent;

            await DataService.LoadAsync(mainStack, async () => await this.LoadMoreAsync(_post, e), _applicationStyling.HighlightColor.ToMauiColor());

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