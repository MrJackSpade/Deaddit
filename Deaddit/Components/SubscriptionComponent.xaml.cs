using Deaddit.Components.ComponentModels;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Models.ThingDefinitions;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Extensions;
using Deaddit.Interfaces;

namespace Deaddit.Components
{
    public partial class SubscriptionComponent : ContentView, ISelectionGroupItem
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly SelectionGroup _selectionGroup;

        private readonly ThingDefinition _subscriptionThing;

        public bool Selected { get; private set; }

        public bool SelectEnabled { get; private set; }

        public event EventHandler<SubRedditSubscriptionRemoveEventArgs>? OnRemove;

        public SubscriptionComponent(ThingDefinition subscriptionThing, bool removable, IAppNavigator appNavigator, ApplicationStyling applicationTheme, SelectionGroup selectionTracker)
        {
            SelectEnabled = removable;
            _appNavigator = appNavigator;
            _applicationStyling = applicationTheme;
            _selectionGroup = selectionTracker;
            _subscriptionThing = subscriptionThing;

            BindingContext = new SubRedditComponentViewModel(subscriptionThing.DisplayName, applicationTheme);
            this.InitializeComponent();

            if (!removable)
            {
                settingsButton.IsVisible = false;
            }
        }

        public void OnRemoveClick(object? sender, EventArgs e)
        {
            OnRemove?.Invoke(this, new SubRedditSubscriptionRemoveEventArgs(this, _subscriptionThing));
        }

        public void OnRemoveClicked(object? sender, EventArgs e)
        {
            // Handle the Share button click
        }

        public async void OnSettingsClick(object? sender, EventArgs e)
        {
            await _selectionGroup.Toggle(this);
        }

        Task ISelectionGroupItem.Select()
        {
            Selected = true;
            BackgroundColor = _applicationStyling.HighlightColor.ToMauiColor();
            actionButtonsStack.IsVisible = true;
            return Task.CompletedTask;
        }

        Task ISelectionGroupItem.Unselect()
        {
            Selected = false;
            BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
            actionButtonsStack.IsVisible = false;
            return Task.CompletedTask;
        }

        private async void OnParentTapped(object? sender, TappedEventArgs e)
        {
            await _appNavigator.OpenThing(_subscriptionThing);
        }
    }
}