using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Extensions;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("reddit-post")]
    public class RedditPostWebComponent : WebComponent
    {
        private readonly ApiPost _post;

        public RedditPostWebComponent(ApiPost post, ApplicationStyling applicationStyling)
        {
            _post = post;

            DivComponent container = new()
            {
                Display = "flex",
                FlexDirection = "row",
                Width = "100%",
                BackgroundColor = applicationStyling.SecondaryColor.ToHex(),
                
            };

            ImgComponent thumbnail = new()
            {
                Src = post.TryGetPreview(),
                Width = $"{applicationStyling.ThumbnailSize}px",
                Height = $"{applicationStyling.ThumbnailSize}px",
                ObjectFit = "cover"
            };

            DivComponent textContainer = new()
            {
                Display = "flex",
                FlexDirection = "column",
                Padding = "10px"
            };

            SpanComponent title = new()
            {
                InnerText = post.Title,
                FontSize = $"{applicationStyling.FontSize}px",
                Color = applicationStyling.TextColor.ToHex(),
            };

            SpanComponent timeUser = new()
            {
                InnerText = $"{post.CreatedUtc.Elapsed()} by {post.Author}",
                FontSize = $"{(int)(applicationStyling.FontSize * 0.75)}px",
                Color = applicationStyling.SubTextColor.ToHex(),
            };

            SpanComponent metaData = new()
            {
                InnerText = $"{post.NumComments} comments {post.SubReddit}",
                FontSize = $"{(int)(applicationStyling.FontSize * 0.75)}px",
                Color = applicationStyling.SubTextColor.ToHex(),
            };

            textContainer.Children.Add(title);
            textContainer.Children.Add(timeUser);
            textContainer.Children.Add(metaData);

            container.Children.Add(thumbnail);
            container.Children.Add(textContainer);

            this.Children.Add(container);
        }
    }
}
