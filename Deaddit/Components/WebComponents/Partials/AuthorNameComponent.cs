using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Components;
using Reddit.Api.Models.Enums;

namespace Deaddit.Components.WebComponents.Partials
{
    internal class AuthorNameComponent : SpanComponent
    {
        public AuthorNameComponent(string author, ApplicationStyling applicationStyling, DistinguishedKind distinguishedKind, bool isOp)
        {
            InnerText = author;
            Color = applicationStyling.SubTextColor.ToHex();
            MarginRight = "5px";
            Padding = "2px";
            BorderRadius = "3px";

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