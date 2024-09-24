using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Extensions;

namespace Deaddit.MAUI.Components.Partials
{
    public partial class RedditCommentComponentBottomBar : ContentView
    {
        public event EventHandler? DownvoteClicked;

        public event EventHandler? MoreClicked;

        public event EventHandler? ReplyClicked;

        public event EventHandler? ShareClicked;

        public event EventHandler? UpvoteClicked;

        public RedditCommentComponentBottomBar(ApiComment comment, ApplicationStyling applicationTheme)
        {
            this.InitializeComponent();

            if (comment.UnrepliableReason != UnrepliableReason.None)
            {
                replyButton.IsVisible = false;
            }

            BackgroundColor = applicationTheme.HighlightColor.ToMauiColor();
            downvoteButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            upvoteButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            moreButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            replyButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            shareButton.TextColor = applicationTheme.TextColor.ToMauiColor();
        }

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