using Deaddit.Components.ComponentModels;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Models.Json.Response;
using Deaddit.Pages;

namespace Deaddit.Components
{
    public partial class SubRedditComponent : ContentView, ISelectable
    {
        private readonly IAppTheme _appTheme;

        private readonly IMarkDownService _markDownService;

        private readonly RedditPost _post;

        private readonly SubRedditComponentViewModel _postViewModel;

        private readonly IRedditClient _redditClient;

        private readonly ISelectionTracker _selectionTracker;

        private readonly string _sort;

        private readonly string _subreddit;

        public SubRedditComponent(string displayString, string subreddit, string sort, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            _redditClient = redditClient;
            _appTheme = appTheme;
            _selectionTracker = selectionTracker;
            _subreddit = subreddit;
            _sort = sort;
            _markDownService = markDownService;

            BindingContext = _postViewModel = new SubRedditComponentViewModel(displayString, appTheme);
            this.InitializeComponent();
        }

        public SubRedditComponent(string subreddit,
                                  string sort,
                                  IRedditClient redditClient,
                                  IAppTheme appTheme,
                                  ISelectionTracker selectionTracker,
                                  IMarkDownService markDownService) : this(subreddit,
                                                                             subreddit,
                                                                             sort,
                                                                             redditClient,
                                                                             appTheme,
                                                                             selectionTracker,
                                                                             markDownService)
        {
        }

        public bool Selected { get; private set; }

        public bool SelectEnabled => true;

        public async void GoButton_Click(object sender, EventArgs e)
        {
            var subredditPage = new SubRedditPage(_subreddit, _sort, _redditClient, _appTheme, _markDownService);
            await Navigation.PushAsync(subredditPage);
            await subredditPage.TryLoad();
        }

        public void OnGoClicked(object sender, EventArgs e)
        {
            // Handle the Save button click
        }

        public void OnRemoveClick(object sender, EventArgs e)
        {
        }

        public void OnRemoveClicked(object sender, EventArgs e)
        {
            // Handle the Share button click
        }

        void ISelectable.Select()
        {
            Selected = true;
            BackgroundColor = _appTheme.HighlightColor;
            actionButtonsStack.IsVisible = true;
        }

        void ISelectable.Unselect()
        {
            Selected = false;
            BackgroundColor = _appTheme.SecondaryColor;
            actionButtonsStack.IsVisible = false;
        }

        private void OnParentTapped(object sender, TappedEventArgs e)
        {
            _selectionTracker.Toggle(this);
        }
    }
}