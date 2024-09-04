using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Exceptions;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components.ComponentModels;
using Deaddit.MAUI.Extensions;
using Deaddit.MAUI.Pages;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Reddit.Models.Options;
using Deaddit.Utils;

namespace Deaddit.MAUI.Components
{
    public partial class RedditPostComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private readonly RedditPost _post;

        private readonly RedditPostComponentViewModel _postViewModel;

        public event EventHandler<BlockRule>? OnBlockAdded;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly IVisitTracker _visitTracker;

        private readonly bool _isPreview;
        private RedditPostComponent(RedditPost post, bool isPreview, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _applicationTheme = applicationTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _selectionGroup = selectionTracker;
            _visitTracker = visitTracker;
            _post = post;
            _isPreview = isPreview;
            _configurationService = configurationService;

            SelectEnabled = isPreview;

            double opacity = isPreview && visitTracker.HasVisited(post) ? applicationTheme.VisitedOpacity : 1;
            bool bodyVisible = !isPreview && !string.IsNullOrWhiteSpace(post.Body);

            BindingContext = _postViewModel = new RedditPostComponentViewModel(post, bodyVisible, opacity, applicationTheme);
            this.InitializeComponent();
            timeUser.IsVisible = !isPreview;

        }

        public bool Selected { get; private set; }

        public bool SelectEnabled { get; private set; }

        public static RedditPostComponent ListView(RedditPost post, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {

            RedditPostComponent toReturn = new(post, true, redditClient, applicationTheme, visitTracker, selectionTracker, blockConfiguration, configurationService);
            return toReturn;
        }

        public static RedditPostComponent PostView(RedditPost post, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditPostComponent toReturn = new(post, false, redditClient, applicationTheme, visitTracker, selectionTracker, blockConfiguration, configurationService);
            return toReturn;
        }

        public async void OnCommentsClicked(object sender, EventArgs e)
        {
            if (_post.IsSelf && _isPreview)
            {
                _postViewModel.Opacity = _applicationTheme.VisitedOpacity;
                _visitTracker.Visit(_post);
            }

            PostPage postPage = new (_post, _redditClient, _applicationTheme, _visitTracker, _blockConfiguration, _configurationService);
            await Navigation.PushAsync(postPage);
            await postPage.TryLoad();
        }

        public void OnDownvoteClicked(object sender, EventArgs e)
        {
            if (_post.Likes == UpvoteState.Downvote)
            {
                _post.Likes = UpvoteState.None;
                _postViewModel.TryAdjustScore(1);
            }
            else
            {
                _post.Likes = UpvoteState.Downvote;
                _postViewModel.TryAdjustScore(-1);
            }

            _postViewModel.SetUpvoteState(_post.Likes);
            _redditClient.SetUpvoteState(_post, _post.Likes);
        }

        public void OnHideClicked(object sender, EventArgs e)
        {
            // Handle the Hide button click
        }

        public async void OnMoreOptionsClicked(object sender, EventArgs e)
        {
            Dictionary<PostMoreOptions, string> options = new();

            Uri.TryCreate(_post.Domain, UriKind.Absolute, out Uri uri);
            
            options.Add(PostMoreOptions.BlockAuthor, $"Block /u/{_post.Author}");
            options.Add(PostMoreOptions.BlockSubreddit, $"Block /r/{_post.SubReddit}");
            options.Add(PostMoreOptions.ViewAuthor, $"View /u/{_post.Author}");
            options.Add(PostMoreOptions.ViewSubreddit, $"View /r/{_post.SubReddit}");

            if (uri != null)
            {
                options.Add(PostMoreOptions.BlockDomain, $"Block {uri.Host}");
            }

            PostMoreOptions? postMoreOptions = await this.DisplayActionSheet("Select:", null, null, options);

            if (postMoreOptions is null)
            {
                return;
            }

            switch (postMoreOptions.Value)
            {
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
                    SubRedditPage page = new(_post.SubReddit, "hot", _redditClient, _applicationTheme, _visitTracker, _blockConfiguration, _configurationService);
                    await Navigation.PushAsync(page);
                    await page.TryLoad();
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
                    if (uri != null)
                    {
                        await this.NewBlockRule(new BlockRule()
                        {
                            Domain = uri.Host,
                            BlockType = BlockType.Post,
                            RuleName = $"({uri.Host})"
                        });
                    }

                    break;
                default: throw new UnhandledEnumException(postMoreOptions.Value);
            }
        }

        public void OnSaveClicked(object sender, EventArgs e)
        {
            // Handle the Save button click
        }

        public async void OnShareClicked(object sender, EventArgs e)
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = _post.Url,
                Title = _post.Title
            });
        }

        public async void OnThumbnailImageClicked(object sender, EventArgs e)
        {
            if (_isPreview)
            {
                _postViewModel.Opacity = _applicationTheme.VisitedOpacity;
                _visitTracker.Visit(_post);
            }

            await Navigation.OpenPost(_post, _redditClient, _applicationTheme, _visitTracker, _blockConfiguration, _configurationService);
        }

        public void OnUpvoteClicked(object sender, EventArgs e)
        {
            if (_post.Likes == UpvoteState.Upvote)
            {
                _post.Likes = UpvoteState.None;
                _postViewModel.TryAdjustScore(-1);
            }
            else
            {
                _post.Likes = UpvoteState.Upvote;
                _postViewModel.TryAdjustScore(1);
            }

            _postViewModel.SetUpvoteState(_post.Likes);
            _redditClient.SetUpvoteState(_post, _post.Likes);
        }

        void ISelectionGroupItem.Select()
        {
            Selected = true;
            BackgroundColor = _applicationTheme.HighlightColor;
            mainGrid.BackgroundColor = _applicationTheme.HighlightColor;
            timeUser.IsVisible = true;
            actionButtonsStack.IsVisible = true;
        }

        void ISelectionGroupItem.Unselect()
        {
            Selected = false;
            BackgroundColor = _applicationTheme.SecondaryColor;
            mainGrid.BackgroundColor = _applicationTheme.SecondaryColor;
            timeUser.IsVisible = false;
            actionButtonsStack.IsVisible = false;
        }

        private void BlockRuleOnSave(object? sender, MAUI.EventArguments.ObjectEditorSaveEventArgs e)
        {
            if (e.Saved is BlockRule blockRule)
            {
                _blockConfiguration.BlockRules.Add(blockRule);

                _configurationService.Write(_blockConfiguration);

                OnBlockAdded?.Invoke(this, blockRule);

            }
        }

        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = new(blockRule, _applicationTheme);

            objectEditorPage.OnSave += this.BlockRuleOnSave;

            await Navigation.PushAsync(objectEditorPage);
        }

        private void OnParentTapped(object sender, TappedEventArgs e)
        {
            _selectionGroup.Toggle(this);
        }
    }
}