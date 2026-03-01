using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Components;
using RedditUser = Reddit.Api.Models.Json.Users.User;

namespace Deaddit.Components.WebComponents.Partials.User
{
    public class TextContainerComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        public TextContainerComponent(RedditUser user, ApplicationStyling applicationStyling)
        {
            _applicationStyling = applicationStyling;

            Display = "flex";
            FlexDirection = "column";
            Padding = "5px";
            FlexGrow = "1";
            OverflowX = "hidden";

            SpanComponent title = new()
            {
                InnerText = user.Subreddit?.PublicDescription,
                FontSize = $"{applicationStyling.CommentFontSize}px",
                OverflowWrap = "break-word"
            };

            Color = applicationStyling.TextColor.ToHex();

            Children.Add(title);

            this.AddMeta($"{user.CommentKarma} comment karma");
            this.AddMeta($"{user.LinkKarma} link karma");
            this.AddMeta($"{user.CreatedUtc} created");
        }

        public event EventHandler Clicked;

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
