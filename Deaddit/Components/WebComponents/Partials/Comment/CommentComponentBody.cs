using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Comment
{
    public class CommentBodyComponent : DivComponent
    {
        public CommentBodyComponent(ApiComment comment, ApplicationStyling applicationStyling)
        {
            InnerText = comment.BodyHtml;
            Color = applicationStyling.TextColor.ToHex();
            FontSize = $"{(int)(applicationStyling.FontSize * .90)}px";
            PaddingLeft = "10px";
        }
    }
}
