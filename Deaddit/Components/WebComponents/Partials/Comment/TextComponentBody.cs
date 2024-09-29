using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Comment
{
    public class HtmlBodyComponent : DivComponent
    {
        public HtmlBodyComponent(string html, ApplicationStyling applicationStyling)
        {
            InnerHTML = html;
            Color = applicationStyling.TextColor.ToHex();
            FontSize = $"{applicationStyling.CommentFontSize}px";
            Padding = "6 0 6 6";
            WordWrap = "break-word";
            OverflowWrap = "break-word";
        }
    }
}