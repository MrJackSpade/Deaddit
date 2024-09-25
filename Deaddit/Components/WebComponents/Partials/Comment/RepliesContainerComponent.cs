using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Comment
{
    public class RepliesContainerComponent : DivComponent
    {
        public RepliesContainerComponent(ApplicationStyling applicationStyling)
        {
            PaddingLeft = "8px";
            BorderLeft = $"1px solid {applicationStyling.SubTextColor.ToHex()}";
        }
    }
}