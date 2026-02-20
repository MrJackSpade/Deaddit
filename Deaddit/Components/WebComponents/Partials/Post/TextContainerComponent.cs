using Deaddit.Components.WebComponents;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Maui.WebComponents.Components;
using Reddit.Api.Models.Api;

namespace Deaddit.Components.WebComponents.Partials.Post
{
    public class TextContainerComponent : DivComponent
    {
        public event EventHandler Clicked;

        public TextContainerComponent(ApiPost post, ApplicationStyling applicationStyling, ApplicationHacks applicationHacks)
        {
            Display = "flex";
            FlexDirection = "column";
            Padding = "5px";
            FlexGrow = "1";
            OverflowX = "hidden";

            SpanComponent title = new()
            {
                InnerText = post.Title,
                FontSize = $"{applicationStyling.TitleFontSize}px",
                OverflowWrap = "break-word"
            };

            switch (post.Distinguished)
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

            DivComponent timeUserContainer = new()
            {
                Display = "none",
                FlexDirection = "row",
                AlignItems = "center"
            };

            SpanComponent timeUser = new()
            {
                InnerText = $"{post.CreatedUtc.Elapsed()} by {post.Author}",
                FontSize = $"{applicationStyling.SubTextFontSize}px",
                Color = applicationStyling.SubTextColor.ToHex()
            };

            timeUserContainer.Children.Add(timeUser);

            SpanComponent metaData = new()
            {
                InnerText = $"{post.NumComments} comments {post.SubRedditName}",
                FontSize = $"{applicationStyling.SubTextFontSize}px",
                Color = applicationStyling.SubTextColor.ToHex(),
            };

            if (!post.IsSelf && Uri.TryCreate(post.Url, UriKind.Absolute, out Uri result) && !string.IsNullOrWhiteSpace(result.Host))
            {
                metaData.InnerText += $" ({result.Host})";
            }

            Children.Add(title);

            string? cleanedLinkFlair = applicationHacks.CleanFlair(post.LinkFlairText);
            if (!string.IsNullOrWhiteSpace(cleanedLinkFlair))
            {
                string color = post.LinkFlairBackgroundColor?.ToHex() ?? applicationStyling.TextColor.ToHex();
                FlairComponent linkFlair = new(cleanedLinkFlair, color, applicationStyling)
                {
                    AlignSelf = "flex-start"
                };
                Children.Add(linkFlair);
            }

            Children.Add(metaData);
            Children.Add(timeUserContainer);

            OnClick += (s, e) => Clicked?.Invoke(this, EventArgs.Empty);
        }

        public void ShowTimeUser(bool show)
        {
            DivComponent? timeUserContainer = Children.OfType<DivComponent>().LastOrDefault();
            if (timeUserContainer != null)
            {
                timeUserContainer.Display = show ? "flex" : "none";
            }
        }
    }
}