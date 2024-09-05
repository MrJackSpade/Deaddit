using Deaddit.Configurations.Models;
using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.Components.Partials
{
    public partial class RedditCommentComponentTopBar : ContentView
    {
        public RedditCommentComponentTopBar(ApiComment comment, ApplicationStyling applicationTheme)
        {
            BackgroundColor = applicationTheme.HighlightColor;

            this.InitializeComponent();

            doneButton.TextColor = applicationTheme.TextColor;
            hideButton.TextColor = applicationTheme.TextColor;
            rootButton.TextColor = applicationTheme.TextColor;
            parentButton.TextColor = applicationTheme.TextColor;
        }

        public event EventHandler? DoneClicked;

        public event EventHandler? HideClicked;

        public event EventHandler? ParentClicked;

        public void OnDoneClicked(object? sender, EventArgs e)
        {
            DoneClicked?.Invoke(this, e);
        }

        public void OnHideClicked(object? sender, EventArgs e)
        {
            HideClicked?.Invoke(this, e);
        }

        public void OnParentClicked(object? sender, EventArgs e)
        {
            ParentClicked?.Invoke(this, e);
        }
    }
}