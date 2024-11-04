using Deaddit.Core.Configurations.Models;
using Reddit.Api.Models.Api;
using Maui.WebComponents.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                case DistinguishedKind.None:
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
