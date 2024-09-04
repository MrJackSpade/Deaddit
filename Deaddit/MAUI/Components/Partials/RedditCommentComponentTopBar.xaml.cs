using Deaddit.Configurations.Models;
using Deaddit.MAUI.Components.ComponentModels;
using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.Components.Partials
{
    public partial class RedditCommentComponentTopBar : ContentView
    {
        public RedditCommentComponentTopBar(ApiComment comment, ApplicationTheme applicationTheme)
        {
            BindingContext = new RedditCommentComponentViewModel(comment, applicationTheme);
            this.InitializeComponent();
        }

        public event EventHandler? DoneClicked;

        public event EventHandler? HideClicked;

        public event EventHandler? ParentClicked;

        public void OnDoneClicked(object sender, EventArgs e)
        {
            DoneClicked?.Invoke(this, e);
        }

        public void OnHideClicked(object sender, EventArgs e)
        {
            HideClicked?.Invoke(this, e);
        }

        public void OnParentClicked(object sender, EventArgs e)
        {
            ParentClicked?.Invoke(this, e);
        }
    }
}