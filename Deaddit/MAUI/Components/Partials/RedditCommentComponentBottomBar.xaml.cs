using Deaddit.Configurations.Models;
using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.Components.Partials
{
    public partial class RedditCommentComponentBottomBar : ContentView
    {
        public RedditCommentComponentBottomBar(ApiComment comment, ApplicationStyling applicationTheme)
        {
            this.InitializeComponent();

            BackgroundColor = applicationTheme.HighlightColor;
            downvoteButton.TextColor = applicationTheme.TextColor;
            upvoteButton.TextColor = applicationTheme.TextColor;
            moreButton.TextColor = applicationTheme.TextColor;
            replyButton.TextColor = applicationTheme.TextColor;
            shareButton.TextColor = applicationTheme.TextColor;
        }

        public event EventHandler? DownvoteClicked;

        public event EventHandler? MoreClicked;

        public event EventHandler? ReplyClicked;

        public event EventHandler? ShareClicked;

        public event EventHandler? UpvoteClicked;

        public void OnDownvoteClicked(object? sender, EventArgs e)
        {
            DownvoteClicked?.Invoke(this, e);
        }

        public void OnMoreClicked(object? sender, EventArgs e)
        {
            MoreClicked?.Invoke(this, e);
        }

        public void OnReplyClicked(object? sender, EventArgs e)
        {
            ReplyClicked?.Invoke(this, e);
        }

        public void OnUpvoteClicked(object? sender, EventArgs e)
        {
            UpvoteClicked?.Invoke(this, e);
        }

        private void OnShareClicked(object? sender, EventArgs e)
        {
            ShareClicked?.Invoke(this, e);
        }
    }
}