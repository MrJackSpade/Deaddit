using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Components;
using Maui.WebComponents.Extensions;
using Reddit.Api.Models.Json.Listings;
using System.Web;

namespace Deaddit.Components.WebComponents
{
    public class RichTextFlairComponent : SpanComponent
    {
        public RichTextFlairComponent(IEnumerable<FlairRichtext> richtext, string highlightColor, ApplicationStyling applicationStyling)
        {
            FontSize = $"{applicationStyling.SubTextFontSize}px";
            Color = highlightColor;
            BackgroundColor = applicationStyling.PrimaryColor.ToHex();
            Display = "inline-flex";
            AlignItems = "center";
            Padding = "4px";
            BorderRadius = "4px";
            BorderColor = highlightColor;
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
                        InnerText = HttpUtility.HtmlEncode(item.Text)
                    };
                    Children.Add(textSpan);
                }
            }
        }
    }
}
