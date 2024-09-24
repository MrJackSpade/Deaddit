using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents
{
    public class FlairComponent : SpanComponent
    {
        public FlairComponent(string text, string highlightColor, ApplicationStyling applicationStyling)
        {
            InnerText = text;
            FontSize = $"{applicationStyling.FontSize * 0.75}px";
            Color = highlightColor;
            BackgroundColor = applicationStyling.PrimaryColor.ToHex();
            Display = "inline";
            Padding = "4px";
            BorderRadius = "4px";
            BorderColor = highlightColor;
            Margin = "2px";
        }
    }
}