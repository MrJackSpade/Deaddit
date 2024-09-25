using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Interfaces;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Comment
{
    public class BottomBarComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        public event EventHandler? OnDownvoteClicked;

        public event EventHandler? OnMoreClicked;

        public event EventHandler? OnReplyClicked;

        public event EventHandler? OnShareClicked;

        public event EventHandler? OnUpvoteClicked;

        public BottomBarComponent(ApplicationStyling applicationStyling)
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
                FontSize = $"{_applicationStyling.FontSize}px",
                Color = _applicationStyling.TextColor.ToHex(),
                BackgroundColor = _applicationStyling.HighlightColor.ToHex(),
                Padding = "10px",
                FlexGrow = "1",
                Border = "0",
            };
        }

        private void DownvoteClicked(object? sender, EventArgs e)
        {
            OnDownvoteClicked?.Invoke(this, e);
        }

        private void InitializeButtons()
        {
            ButtonComponent upvoteButton = this.CreateActionButton("▲");
            ButtonComponent downvoteButton = this.CreateActionButton("▼");
            ButtonComponent moreButton = this.CreateActionButton("...");
            ButtonComponent shareButton = this.CreateActionButton("⢔");
            ButtonComponent replyButton = this.CreateActionButton("↩");

            upvoteButton.OnClick += this.UpvoteClicked;
            downvoteButton.OnClick += this.DownvoteClicked;
            moreButton.OnClick += this.MoreClicked;
            shareButton.OnClick += this.ShareClicked;
            replyButton.OnClick += this.ReplyClicked;

            Children.Add(upvoteButton);
            Children.Add(downvoteButton);
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

        private void ShareClicked(object? sender, EventArgs e)
        {
            OnShareClicked?.Invoke(this, e);
        }

        private void UpvoteClicked(object? sender, EventArgs e)
        {
            OnUpvoteClicked?.Invoke(this, e);
        }
    }
}