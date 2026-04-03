using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Maui.WebComponents.Components;
using Reddit.Api.Models.Enums;

namespace Deaddit.Components.WebComponents.Partials.Post
{
    public class TextContainerComponent : DivComponent
    {
        public TextContainerComponent(ApiPost post, ApplicationStyling applicationStyling, ApplicationHacks applicationHacks)
        {
            Display = "flex";
            FlexDirection = "column";
            Padding = "0px 5px 5px 5px";
            FlexGrow = "1";
            OverflowX = "hidden";

            SpanComponent title = new()
            {
                InnerText = post.Title,
                FontSize = $"{applicationStyling.TitleFontSize}px",
                OverflowWrap = "break-word",
                Margin = "0",
                Padding = "0"
            };

            switch (post.Distinguished)
            {
                case DistinguishedKind.Null:
                case DistinguishedKind.Empty:
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
                InnerText = $"{post.NumComments} 🗨 {post.SubRedditName}",
                FontSize = $"{applicationStyling.SubTextFontSize}px",
                Color = applicationStyling.SubTextColor.ToHex(),
            };

            if (!post.IsSelf && Uri.TryCreate(post.Url, UriKind.Absolute, out Uri result) && !UrlHelper.IsHiddenHost(result.Host))
            {
                metaData.InnerText += $" ({result.Host})";
            }

            Children.Add(title);

            string? flairBackgroundColor = post.LinkFlairBackgroundColor.ToHex();
            string flairTextColor = post.LinkFlairTextColor.ToFlairTextHex(applicationStyling);
            if (applicationHacks.ShouldResolveFlairImages() && post.LinkFlairRichText.Count > 0)
            {
                RichTextFlairComponent linkFlair = new(post.LinkFlairRichText, flairTextColor, applicationStyling, flairBackgroundColor)
                {
                    AlignSelf = "flex-start"
                };
                if (linkFlair.Children.Count > 0)
                {
                    Children.Add(linkFlair);
                }
            }
            else
            {
                string? cleanedLinkFlair = applicationHacks.CleanFlair(post.LinkFlairText);
                if (!string.IsNullOrWhiteSpace(cleanedLinkFlair))
                {
                    FlairComponent linkFlair = new(cleanedLinkFlair, flairTextColor, applicationStyling, flairBackgroundColor)
                    {
                        AlignSelf = "flex-start"
                    };
                    Children.Add(linkFlair);
                }
            }

            Children.Add(metaData);
            Children.Add(timeUserContainer);

            OnClick += (s, e) => Clicked?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Clicked;

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