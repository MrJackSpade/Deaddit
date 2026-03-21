using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Components;
using System.Web;

namespace Deaddit.Components.WebComponents
{
    public class FlairComponent : SpanComponent
    {
        public FlairComponent(string text, string textColor, ApplicationStyling applicationStyling, string? flairBackgroundColor = null)
        {
            InnerText = HttpUtility.HtmlEncode(text);
            FontSize = $"{applicationStyling.SubTextFontSize}px";
            Display = "inline";
            Padding = "4px";
            BorderRadius = "4px";
            Margin = "2px";

            string bgColor = flairBackgroundColor ?? applicationStyling.PrimaryColor.ToHex();

            if (applicationStyling.SwapFlairColors && flairBackgroundColor != null)
            {
                Color = bgColor;
                BackgroundColor = applicationStyling.PrimaryColor.ToHex();
                BorderColor = bgColor;
            }
            else
            {
                Color = textColor;
                BackgroundColor = bgColor;
                BorderColor = textColor;
            }
        }
    }
}