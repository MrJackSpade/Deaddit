using Deaddit.Core.Configurations.Models;
using Reddit.Api.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials.Post
{
    public class ActionButtonsComponent : DivComponent
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly ApiPost _post;

        private readonly ButtonComponent _saveButton;

        public event EventHandler CommentsClicked;

        public event EventHandler HideClicked;

        public event EventHandler MoreClicked;

        public event EventHandler SaveClicked;

        public event EventHandler ShareClicked;

        public ActionButtonsComponent(ApplicationStyling applicationStyling, ApiPost post)
        {
            _applicationStyling = applicationStyling;
            _post = post;

            Display = "none";
            FlexDirection = "row";
            Width = "100%";
            BackgroundColor = applicationStyling.HighlightColor.ToHex();

            ButtonComponent shareButton = this.CreateActionButton("Share");
            _saveButton = this.CreateActionButton(post.Saved == true ? "Unsave" : "Save");
            ButtonComponent hideButton = this.CreateActionButton("Hide");
            ButtonComponent moreButton = this.CreateActionButton("...");
            ButtonComponent commentsButton = this.CreateActionButton("🗨");

            shareButton.OnClick += (s, e) => ShareClicked?.Invoke(this, EventArgs.Empty);
            _saveButton.OnClick += (s, e) => SaveClicked?.Invoke(this, EventArgs.Empty);
            hideButton.OnClick += (s, e) => HideClicked?.Invoke(this, EventArgs.Empty);
            moreButton.OnClick += (s, e) => MoreClicked?.Invoke(this, EventArgs.Empty);
            commentsButton.OnClick += (s, e) => CommentsClicked?.Invoke(this, EventArgs.Empty);

            Children.Add(shareButton);
            Children.Add(_saveButton);
            Children.Add(hideButton);
            Children.Add(moreButton);
            Children.Add(commentsButton);
        }

        public void UpdateSaveButtonText()
        {
            _saveButton.InnerText = _post.Saved == true ? "Unsave" : "Save";
        }

        private ButtonComponent CreateActionButton(string text)
        {
            return new ButtonComponent
            {
                InnerText = text,
                FontSize = $"{_applicationStyling.TitleFontSize}px",
                Color = _applicationStyling.TextColor.ToHex(),
                BackgroundColor = "transparent",
                Padding = "10px",
                FlexGrow = "1",
                Border = "0",
            };
        }
    }
}