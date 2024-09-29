using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Post
{
    internal class PostBodyComponent : DivComponent
    {
        public PostBodyComponent()
        {
        }

        public PostBodyComponent(ApiPost post, ApplicationStyling applicationStyling)
        {
            BorderColor = applicationStyling.TertiaryColor.ToHex();
            BorderWidth = "2px";
            Color = applicationStyling.TextColor.ToHex();
            Padding = "5px";
            BoxSizing = "border-box";
            FontSize = $"{applicationStyling.TitleFontSize}px";
            Border = $"1px solid {applicationStyling.TextColor.ToHex()}";
            BackgroundColor = applicationStyling.PrimaryColor.ToHex();

            InnerText = post.BodyHtml;
        }
    }
}