using Deaddit.Core.Configurations.Models;
using Deaddit.Interfaces;
using Maui.WebComponents.Components;
using Reddit.Api.Models.Api;

namespace Deaddit.Components.WebComponents.Partials.User
{
    public class UserHeader : DivComponent
    {
        private readonly ApiUser _userData;

        private readonly IAppNavigator _appNavigator;
        public UserHeader(ApiUser userData, IAppNavigator appNavigator, ApplicationStyling applicationStyling, bool showMessage)
        {
            _userData = userData;
            _appNavigator = appNavigator;

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

            if (showMessage)
            {
                ButtonComponent messageButton = new()
                {
                    InnerHTML = "&#128488;",
                    BackgroundColor = "transparent",
                    Color = applicationStyling.TextColor.ToHex(),
                    Width = $"{applicationStyling.ThumbnailSize}px",
                    Height = $"{applicationStyling.ThumbnailSize}px",
                    FontSize = $"{applicationStyling.ThumbnailSize}px",
                    Display = "flex",
                    JustifyContent = "center",
                    AlignItems = "center",
                    Border = "none"
                };

                topContainer.Children.Add(messageButton);
                messageButton.OnClick += this.MessageButton_OnClick;
            }

            Children.Add(topContainer);
        }

        private void MessageButton_OnClick(object? sender, EventArgs e)
        {
            _appNavigator.OpenMessagePage(_userData);
        }
    }
}