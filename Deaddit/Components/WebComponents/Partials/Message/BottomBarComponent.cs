using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Message
{
    public class MessageBarComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        public event EventHandler? OnMoreClicked;

        public event EventHandler? OnReplyClicked;

        public MessageBarComponent(ApplicationStyling applicationStyling)
        {
            _applicationStyling = applicationStyling;

            Display = "none";
            this.InitializeButtons();
        }

        private ButtonComponent CreateActionButton(string text)
        {
            return new ButtonComponent
            {
                InnerText = text,
                FontSize = $"{_applicationStyling.TitleFontSize}px",
                Color = _applicationStyling.TextColor.ToHex(),
                BackgroundColor = _applicationStyling.HighlightColor.ToHex(),
                Padding = "10px",
                FlexGrow = "1",
                Border = "0",
            };
        }

        private void InitializeButtons()
        {
            ButtonComponent moreButton = this.CreateActionButton("...");
            ButtonComponent shareButton = this.CreateActionButton("⢔");
            ButtonComponent replyButton = this.CreateActionButton("↩");

            moreButton.OnClick += this.MoreClicked;
            replyButton.OnClick += this.ReplyClicked;

            Children.Add(moreButton);
            Children.Add(shareButton);
            Children.Add(replyButton);
        }

        private void MoreClicked(object? sender, EventArgs e)
        {
            OnMoreClicked?.Invoke(this, e);
        }

        private void ReplyClicked(object? sender, EventArgs e)
        {
            OnReplyClicked?.Invoke(this, e);
        }
    }
}