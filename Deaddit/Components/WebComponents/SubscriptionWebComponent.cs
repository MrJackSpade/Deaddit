using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Utils;
using Deaddit.EventArguments;
using Deaddit.Interfaces;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using Reddit.Api.Models.ThingDefinitions;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("subscription-item")]
    public class SubscriptionWebComponent : DivComponent, ISelectionGroupItem, ISubscriptionComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly IAppNavigator _appNavigator;

        private readonly DivComponent _actionButtons;

        private readonly SelectionGroup? _selectionGroup;

        private readonly ThingDefinition _subscriptionThing;

        private readonly string _backgroundColor;

        private readonly string _highlightColor;

        public bool SelectEnabled { get; }

        public event EventHandler<SubRedditSubscriptionRemoveEventArgs>? OnRemove;

        public SubscriptionWebComponent(ThingDefinition subscriptionThing, bool removable, IAppNavigator appNavigator, ApplicationStyling applicationStyling, SelectionGroup? selectionGroup)
        {
            SelectEnabled = removable;
            _appNavigator = appNavigator;
            _applicationStyling = applicationStyling;
            _selectionGroup = selectionGroup;
            _subscriptionThing = subscriptionThing;

            _backgroundColor = applicationStyling.SecondaryColor.ToHex();
            _highlightColor = applicationStyling.HighlightColor.ToHex();

            Display = "flex";
            FlexDirection = "column";
            BackgroundColor = _backgroundColor;

            DivComponent mainRow = new()
            {
                Display = "flex",
                FlexDirection = "row",
                Width = "100%",
                AlignItems = "center"
            };

            SpanComponent label = new()
            {
                InnerText = subscriptionThing.DisplayName,
                Color = applicationStyling.TextColor.ToHex(),
                Padding = "10px",
                FlexGrow = "1",
                Cursor = "pointer"
            };
            label.OnClick += this.Label_OnClick;

            mainRow.Children.Add(label);

            if (removable)
            {
                SpanComponent settingsButton = new()
                {
                    InnerText = "\u2699",
                    Color = applicationStyling.TextColor.ToHex(),
                    Padding = "10px",
                    Cursor = "pointer"
                };
                settingsButton.OnClick += this.SettingsButton_OnClick;
                mainRow.Children.Add(settingsButton);
            }

            Children.Add(mainRow);

            _actionButtons = new DivComponent()
            {
                Display = "none",
                FlexDirection = "row"
            };

            SpanComponent removeButton = new()
            {
                InnerText = "Remove",
                Color = applicationStyling.TextColor.ToHex(),
                Padding = "10px",
                Cursor = "pointer"
            };
            removeButton.OnClick += this.RemoveButton_OnClick;
            _actionButtons.Children.Add(removeButton);

            Children.Add(_actionButtons);
        }

        public Task Select()
        {
            BackgroundColor = _highlightColor;
            _actionButtons.Display = "flex";
            return Task.CompletedTask;
        }

        public Task Unselect()
        {
            BackgroundColor = _backgroundColor;
            _actionButtons.Display = "none";
            return Task.CompletedTask;
        }

        private async void Label_OnClick(object? sender, EventArgs e)
        {
            await _appNavigator.OpenThing(_subscriptionThing);
        }

        private async void SettingsButton_OnClick(object? sender, EventArgs e)
        {
            if (_selectionGroup != null)
            {
                await _selectionGroup.Toggle(this);
            }
        }

        private void RemoveButton_OnClick(object? sender, EventArgs e)
        {
            OnRemove?.Invoke(this, new SubRedditSubscriptionRemoveEventArgs(this, _subscriptionThing));
        }
    }
}
