using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Components;
using Maui.WebComponents.Extensions;
using Reddit.Api.Models.Json.Listings;
using System.Web;

namespace Deaddit.Components.WebComponents
{
    public class RichTextFlairComponent : SpanComponent
    {
        public RichTextFlairComponent(IEnumerable<FlairRichtext> richtext, string textColor, ApplicationStyling applicationStyling, string? flairBackgroundColor = null)
        {
            FontSize = $"{applicationStyling.SubTextFontSize}px";

            string bgColor = flairBackgroundColor ?? applicationStyling.PrimaryColor.ToHex();

            if (applicationStyling.SwapFlairColors && flairBackgroundColor != null)
            {
                Color = bgColor;
                BackgroundColor = applicationStyling.PrimaryColor.ToHex();
            }
            else
            {
                Color = textColor;
                BackgroundColor = bgColor;
            }
            Display = "inline-flex";
            AlignItems = "center";
            Padding = "4px";
            BorderRadius = "4px";
            BorderColor = Color;
            Margin = "2px";
            this.Style("gap", "2px");

            foreach (FlairRichtext item in richtext)
            {
                if (item.Type == "emoji" && !string.IsNullOrWhiteSpace(item.Url))
                {
                    string imageSize = $"{(int)(applicationStyling.SubTextFontSize * 1.2)}px";
                    ImgComponent img = new()
                    {
                        Src = item.Url,
                        Height = imageSize,
                        Width = imageSize
                    };
                    img.Style("vertical-align", "middle");
                    Children.Add(img);
                }
                else if (!string.IsNullOrWhiteSpace(item.Text))
                {
                    SpanComponent textSpan = new()
                    {
                        InnerText = item.Text
                    };
                    Children.Add(textSpan);
                }
            }
        }
    }
}
