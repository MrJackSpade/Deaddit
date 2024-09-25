using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            BackgroundColor = applicationStyling.PrimaryColor.ToHex();
            InnerText = post.BodyHtml;
        }
    }
}