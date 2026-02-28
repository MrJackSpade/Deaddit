using Deaddit.Core.Configurations.Models;
using Reddit.Api.Models.Enums;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials
{
    class AuthorNameComponent : SpanComponent
    {
        public AuthorNameComponent(string author, ApplicationStyling applicationStyling, DistinguishedKind distinguishedKind, bool isOp)
        {
            InnerText = author;
            Color = applicationStyling.SubTextColor.ToHex();
            MarginRight = "5px";

            switch (distinguishedKind)
            {
                case DistinguishedKind.Null:
                case DistinguishedKind.Empty:
                    if (isOp)
                    {
                        BackgroundColor = applicationStyling.OpBackgroundColor.ToHex();
                        Color = applicationStyling.OpTextColor.ToHex();
                    }

                    break;

                case DistinguishedKind.Moderator:
                    BackgroundColor = applicationStyling.ModeratorAuthorBackgroundColor.ToHex();
                    Color = applicationStyling.ModeratorAuthorTextColor.ToHex();
                    break;

                case DistinguishedKind.Admin:
                    BackgroundColor = applicationStyling.AdminAuthorBackgroundColor.ToHex();
                    Color = applicationStyling.AdminAuthorTextColor.ToHex();
                    break;
            }
        }
    }
}
