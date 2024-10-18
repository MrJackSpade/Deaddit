using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

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

            SpanComponent timeUser = new()
            {
                InnerText = $"{post.CreatedUtc.Elapsed()} by {post.Author}",
                FontSize = $"{applicationStyling.SubTextFontSize}px",
                Color = applicationStyling.SubTextColor.ToHex(),
                Display = "none"
            };

            SpanComponent metaData = new()
            {
                InnerText = $"{post.NumComments} comments {post.SubReddit}",
                FontSize = $"{applicationStyling.SubTextFontSize}px",
                Color = applicationStyling.SubTextColor.ToHex(),
            };

            if (!post.IsSelf && Uri.TryCreate(post.Url, UriKind.Absolute, out Uri result))
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
            Children.Add(timeUser);

            OnClick += (s, e) => Clicked?.Invoke(this, EventArgs.Empty);
        }

        public void ShowTimeUser(bool show)
        {
            //This is fucking stupid. How did I end up here?
            SpanComponent? timeUser = Children.OfType<SpanComponent>().LastOrDefault(c => c.InnerText.Contains("by"));
            if (timeUser != null)
            {
                timeUser.Display = show ? "block" : "none";
            }
        }
    }
}