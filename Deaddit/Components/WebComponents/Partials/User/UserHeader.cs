using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.User
{
    public class UserHeader : DivComponent
    {
        private readonly ApiUser _userData;

        public UserHeader(ApiUser userData, ApplicationStyling applicationStyling)
        {
            _userData = userData;

            DivComponent topContainer = new()
            {
                Display = "flex",
                FlexDirection = "row",
                Width = "100%",
            };

            ImgComponent thumbnail = new()
            {
                Src = userData.IconImg,
                Width = $"{applicationStyling.ThumbnailSize}px",
                Height = $"{applicationStyling.ThumbnailSize}px",
                FlexShrink = "0",
                ObjectFit = "cover"
            };

            TextContainerComponent _textContainer = new(userData, applicationStyling);

            topContainer.Children.Add(thumbnail);
            topContainer.Children.Add(_textContainer);

            Children.Add(topContainer);
        }
    }
}