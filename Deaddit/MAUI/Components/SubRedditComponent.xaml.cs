using Deaddit.Configurations.Interfaces;
using Deaddit.Configurations.Models;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components.ComponentModels;
using Deaddit.MAUI.EventArguments;
using Deaddit.MAUI.Pages;
using Deaddit.Reddit.Interfaces;
using Deaddit.Utils;

namespace Deaddit.MAUI.Components
{
    public partial class SubRedditComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly BlockConfiguration _blockConfiguration;

        private readonly IConfigurationService _configurationService;

        private readonly IRedditClient _redditClient;

        private readonly SelectionGroup _selectionGroup;

        private readonly SubRedditSubscription _subscription;

        private readonly IVisitTracker _visitTracker;

        private SubRedditComponent(SubRedditSubscription subscription, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            _redditClient = redditClient;
            _applicationTheme = applicationTheme;
            _configurationService = configurationService;
            _blockConfiguration = blockConfiguration;
            _selectionGroup = selectionTracker;
            _subscription = subscription;
            _visitTracker = visitTracker;

            BindingContext = new SubRedditComponentViewModel(subscription.DisplayString, applicationTheme);
            this.InitializeComponent();
            _configurationService = configurationService;
        }

        public event EventHandler<SubRedditSubscriptionRemoveEventArgs>? OnRemove;

        public bool Selected { get; private set; }

        public bool SelectEnabled { get; private set; }

        public static SubRedditComponent Fixed(SubRedditSubscription subscription, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            return new SubRedditComponent(subscription, redditClient, applicationTheme, visitTracker, selectionTracker, blockConfiguration, configurationService)
            {
                SelectEnabled = false
            };
        }

        public static SubRedditComponent Removable(SubRedditSubscription subscription, IRedditClient redditClient, ApplicationTheme applicationTheme, IVisitTracker visitTracker, SelectionGroup selectionTracker, BlockConfiguration blockConfiguration, IConfigurationService configurationService)
        {
            return new SubRedditComponent(subscription, redditClient, applicationTheme, visitTracker, selectionTracker, blockConfiguration, configurationService)
            {
                SelectEnabled = true
            };
        }

        public async void GoButton_Click(object sender, EventArgs e)
        {
            SubRedditPage subredditPage = new(_subscription.SubReddit, _subscription.Sort, _redditClient, _applicationTheme, _visitTracker, _blockConfiguration, _configurationService);
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

        void ISelectionGroupItem.Select()
        {
            Selected = true;
            BackgroundColor = _applicationTheme.HighlightColor;
            actionButtonsStack.IsVisible = true;
        }

        void ISelectionGroupItem.Unselect()
        {
            Selected = false;
            BackgroundColor = _applicationTheme.SecondaryColor;
            actionButtonsStack.IsVisible = false;
        }

        private void OnParentTapped(object sender, TappedEventArgs e)
        {
            _selectionGroup.Toggle(this);
        }
    }
}