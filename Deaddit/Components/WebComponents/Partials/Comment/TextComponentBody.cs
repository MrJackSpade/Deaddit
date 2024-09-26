using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Comment
{
    public class TextBodyComponent : DivComponent
    {
        public TextBodyComponent(string html, ApplicationStyling applicationStyling)
        {
            InnerText = html;
            Color = applicationStyling.TextColor.ToHex();
            FontSize = $"{applicationStyling.CommentFontSize}px";
            PaddingLeft = "10px";
        }
    }
}
