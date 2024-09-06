using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Extensions;

namespace Deaddit.MAUI.Components.Partials
{
    public partial class RedditCommentComponentTopBar : ContentView
    {
        public RedditCommentComponentTopBar(ApiComment comment, ApplicationStyling applicationTheme)
        {
            BackgroundColor = applicationTheme.HighlightColor.ToMauiColor();

            this.InitializeComponent();

            doneButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            hideButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            rootButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            parentButton.TextColor = applicationTheme.TextColor.ToMauiColor();
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