using Deaddit.Core.Configurations.Models;
using Deaddit.Interfaces;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("history-item")]
    public class HistoryWebComponent : DivComponent
    {
        private readonly IAppNavigator _appNavigator;

        public HistoryWebComponent(IAppNavigator appNavigator, ApplicationStyling applicationStyling)
        {
            _appNavigator = appNavigator;

            Display = "flex";
            FlexDirection = "row";
            BackgroundColor = applicationStyling.SecondaryColor.ToHex();
            Cursor = "pointer";

            SpanComponent label = new()
            {
                InnerText = "History",
                Color = applicationStyling.TextColor.ToHex(),
                Padding = "10px"
            };

            Children.Add(label);

            OnClick += this.Component_OnClick;
        }

        private async void Component_OnClick(object? sender, EventArgs e)
        {
            await _appNavigator.OpenHistory();
        }
    }
}
