using Deaddit.Components.ComponentModels;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;

namespace Deaddit.MAUI.Components
{
    public partial class SubRedditComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly SelectionGroup _selectionGroup;

        private readonly SubRedditSubscription _subscription;

        public SubRedditComponent(SubRedditSubscription subscription, bool removable, IAppNavigator appNavigator, ApplicationStyling applicationTheme, SelectionGroup selectionTracker)
        {
            SelectEnabled = removable;
            _appNavigator = appNavigator;
            _applicationStyling = applicationTheme;
            _selectionGroup = selectionTracker;
            _subscription = subscription;

            BindingContext = new SubRedditComponentViewModel(subscription.DisplayString, applicationTheme);
            this.InitializeComponent();
        }

        public event EventHandler<SubRedditSubscriptionRemoveEventArgs>? OnRemove;

        public bool Selected { get; private set; }

        public bool SelectEnabled { get; private set; }

        public void OnRemoveClick(object? sender, EventArgs e)
        {
            OnRemove.Invoke(this, new SubRedditSubscriptionRemoveEventArgs(_subscription, this));
        }

        public void OnRemoveClicked(object? sender, EventArgs e)
        {
            // Handle the Share button click
        }

        public void OnSettingsClick(object? sender, EventArgs e)
        {
            _selectionGroup.Toggle(this);
        }

        void ISelectionGroupItem.Select()
        {
            Selected = true;
            BackgroundColor = _applicationStyling.HighlightColor.ToMauiColor();
            actionButtonsStack.IsVisible = true;
        }

        void ISelectionGroupItem.Unselect()
        {
            Selected = false;
            BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
            actionButtonsStack.IsVisible = false;
        }

        private async void OnParentTapped(object? sender, TappedEventArgs e)
        {
            await _appNavigator.OpenSubReddit(_subscription.SubReddit, _subscription.Sort);
        }
    }
}