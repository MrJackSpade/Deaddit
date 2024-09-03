using Deaddit.Components.ComponentModels;
using Deaddit.Configurations;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Models;
using Deaddit.Models.Json.Response;
using Deaddit.Pages;
using Deaddit.Services.Configuration;

namespace Deaddit.Components
{
    public partial class RedditPostComponent : ContentView, ISelectable
    {
        private readonly IAppTheme _appTheme;

        private readonly IMarkDownService _markDownService;

        private readonly RedditPost _post;

        private readonly RedditPostComponentViewModel _postViewModel;

        private readonly IRedditClient _redditClient;

        private readonly ISelectionTracker _selectionTracker;

        private readonly IBlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private RedditPostComponent(RedditPost post, bool postBodyIsVisible, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService, IBlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _appTheme = appTheme;
            _blockConfiguration = blockConfiguration;
            _redditClient = redditClient;
            _selectionTracker = selectionTracker;
            _markDownService = markDownService;
            _post = post;

            BindingContext = _postViewModel = new RedditPostComponentViewModel(post, postBodyIsVisible, appTheme, _markDownService);
            this.InitializeComponent();
            _configurationService = configurationService;
        }

        public bool Selected { get; private set; }

        public bool SelectEnabled { get; private set; }

        public static RedditPostComponent ListView(RedditPost post, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService, IBlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditPostComponent toReturn = new(post, false, redditClient, appTheme, selectionTracker, markDownService, blockConfiguration, configurationService)
            {
                SelectEnabled = true
            };
            return toReturn;
        }

        public static RedditPostComponent PostView(RedditPost post, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService, IBlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            RedditPostComponent toReturn = new(post, !string.IsNullOrWhiteSpace(post.Body), redditClient, appTheme, selectionTracker, markDownService, blockConfiguration, configurationService);
            toReturn.timeUser.IsVisible = true;
            toReturn.SelectEnabled = false;
            return toReturn;
        }

        public async void OnCommentsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PostPage(_post, _redditClient, _appTheme, _markDownService, _blockConfiguration, _configurationService));
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

        public void OnImageClicked(object sender, EventArgs e)
        {
        }

        public async void OnMoreOptionsClicked(object sender, EventArgs e)
        {
            PostMoreOptions? postMoreOptions = await this.DisplayActionSheet<PostMoreOptions>("Select:", null, null);

            if(postMoreOptions is null)
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
                default: throw new NotImplementedException();
            }
        }

        private async Task NewBlockRule(BlockRule blockRule)
        {
            ObjectEditorPage objectEditorPage = new(blockRule, _appTheme);

            objectEditorPage.OnSave += this.BlockRuleOnSave;

            await Navigation.PushAsync(objectEditorPage);
        }

        private void BlockRuleOnSave(object? sender, EventArguments.ObjectEditorSaveEventArgs e)
        {
            _blockConfiguration.BlockRules.Add((BlockRule)e.Saved);

            _configurationService.Write(_blockConfiguration);
        }

        public void OnSaveClicked(object sender, EventArgs e)
        {
            // Handle the Save button click
        }

        public void OnShareClicked(object sender, EventArgs e)
        {
            // Handle the Share button click
        }

        public async void OnThumbnailImageClicked(object sender, EventArgs e)
        {
            await Navigation.OpenPost(_post, _redditClient, _appTheme, _markDownService, _blockConfiguration, _configurationService);
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

        void ISelectable.Select()
        {
            Selected = true;
            BackgroundColor = _appTheme.HighlightColor;
            mainGrid.BackgroundColor = _appTheme.HighlightColor;
            timeUser.IsVisible = true;
            actionButtonsStack.IsVisible = true;
        }

        void ISelectable.Unselect()
        {
            Selected = false;
            BackgroundColor = _appTheme.SecondaryColor;
            mainGrid.BackgroundColor = _appTheme.SecondaryColor;
            timeUser.IsVisible = false;
            actionButtonsStack.IsVisible = false;
        }

        private void OnParentTapped(object sender, TappedEventArgs e)
        {
            _selectionTracker.Toggle(this);
        }
    }
}