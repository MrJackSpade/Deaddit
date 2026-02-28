using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.User
{
    public class TextContainerComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        public event EventHandler Clicked;

        public TextContainerComponent(ApiUser user, ApplicationStyling applicationStyling)
        {
            _applicationStyling = applicationStyling;

            Display = "flex";
            FlexDirection = "column";
            Padding = "5px";
            FlexGrow = "1";
            OverflowX = "hidden";

            SpanComponent title = new()
            {
                InnerText = user.SubReddit?.PublicDescription,
                FontSize = $"{applicationStyling.CommentFontSize}px",
                OverflowWrap = "break-word"
            };

            switch (user.Distinguished)
            {
                case DistinguishedKind.None:
                    Color = applicationStyling.TextColor.ToHex();
                    break;

                case DistinguishedKind.Moderator:
                    Color = applicationStyling.ModeratorTitleTextColor.ToHex();
                    break;

                case DistinguishedKind.Admin:
                    Color = applicationStyling.AdminTitleTextColor.ToHex();
                    break;
            }

            Children.Add(title);

            this.AddMeta($"{user.CommentKarma} comment karma");
            this.AddMeta($"{user.LinkKarma} link karma");
            this.AddMeta($"{user.NumComments} comments");
            this.AddMeta($"{user.CreatedUtc} created");
        }

        private void AddMeta(string text)
        {
            SpanComponent metaData = new()
            {
                FontSize = $"{_applicationStyling.SubTextFontSize}px",
                Color = _applicationStyling.SubTextColor.ToHex(),
            };

            Children.Add(metaData);
        }
    }
}