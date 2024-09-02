using Deaddit.Components.ComponentModels;
using Deaddit.Configurations;
using Deaddit.EventArguments;
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

        private readonly SubRedditSubscription _subscription;

        private SubRedditComponent(SubRedditSubscription subscription, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            _redditClient = redditClient;
            _appTheme = appTheme;
            _selectionTracker = selectionTracker;
            _subscription = subscription;
            _markDownService = markDownService;

            BindingContext = _postViewModel = new SubRedditComponentViewModel(subscription.DisplayString, appTheme);
            this.InitializeComponent();
        }

        public event EventHandler<SubRedditSubscriptionRemoveEventArgs> OnRemove;

        public bool Selected { get; private set; }

        public bool SelectEnabled { get; private set; }

        public static SubRedditComponent Fixed(SubRedditSubscription subscription, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            return new SubRedditComponent(subscription, redditClient, appTheme, selectionTracker, markDownService)
            {
                SelectEnabled = false
            };
        }

        public static SubRedditComponent Removable(SubRedditSubscription subscription, IRedditClient redditClient, IAppTheme appTheme, ISelectionTracker selectionTracker, IMarkDownService markDownService)
        {
            return new SubRedditComponent(subscription, redditClient, appTheme, selectionTracker, markDownService)
            {
                SelectEnabled = true
            };
        }

        public async void GoButton_Click(object sender, EventArgs e)
        {
            SubRedditPage subredditPage = new(_subscription.SubReddit, _subscription.Sort, _redditClient, _appTheme, _markDownService);
            await Navigation.PushAsync(subredditPage);
            await subredditPage.TryLoad();
        }

        public void OnGoClicked(object sender, EventArgs e)
        {
            // Handle the Save button click
        }

        public void OnRemoveClick(object sender, EventArgs e)
        {
            OnRemove.Invoke(this, new SubRedditSubscriptionRemoveEventArgs(_subscription, this));
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