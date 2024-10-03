using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Comment
{
    public class BottomBarComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        private ButtonComponent _downvoteButton;

        private ButtonComponent _upvoteButton;

        public event EventHandler? OnDownvoteClicked;

        public event EventHandler? OnMoreClicked;

        public event EventHandler? OnReplyClicked;

        public event EventHandler? OnShareClicked;

        public event EventHandler? OnUpvoteClicked;

        public BottomBarComponent(ApplicationStyling applicationStyling, UpvoteState initialState)
        {
            _applicationStyling = applicationStyling;

            Display = "none";
            this.InitializeButtons();
            this.SetUpvoteState(initialState);
        }

        public void SetUpvoteState(UpvoteState upvote)
        {
            switch (upvote)
            {
                case UpvoteState.None:
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case UpvoteState.Upvote:
                    _upvoteButton.Color = _applicationStyling.UpvoteColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    break;

                case UpvoteState.Downvote:
                    _upvoteButton.Color = _applicationStyling.TextColor.ToHex();
                    _downvoteButton.Color = _applicationStyling.DownvoteColor.ToHex();
                    break;
            }
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

        private void DownvoteClicked(object? sender, EventArgs e)
        {
            OnDownvoteClicked?.Invoke(this, e);
        }

        private void InitializeButtons()
        {
            _upvoteButton = this.CreateActionButton("▲");
            _downvoteButton = this.CreateActionButton("▼");
            ButtonComponent moreButton = this.CreateActionButton("...");
            ButtonComponent shareButton = this.CreateActionButton("⢔");
            ButtonComponent replyButton = this.CreateActionButton("↩");

            _upvoteButton.OnClick += this.UpvoteClicked;
            _downvoteButton.OnClick += this.DownvoteClicked;
            moreButton.OnClick += this.MoreClicked;
            shareButton.OnClick += this.ShareClicked;
            replyButton.OnClick += this.ReplyClicked;

            Children.Add(_upvoteButton);
            Children.Add(_downvoteButton);
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