using Deaddit.Configurations.Models;
using Deaddit.MAUI.Components.ComponentModels;
using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.Components
{
    public partial class MoreCommentsComponent : ContentView
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly ApiComment _comment;

        private readonly MoreCommentsComponentViewModel _commentViewModel;

        public MoreCommentsComponent(ApiComment comment, ApplicationTheme applicationTheme)
        {
            _applicationTheme = applicationTheme;
            _comment = comment;
            BindingContext = _commentViewModel = new MoreCommentsComponentViewModel($"More {comment.Count}", applicationTheme);
            this.InitializeComponent();
        }

        public event EventHandler<ApiComment>? OnClick;

        public async void OnParentTapped(object sender, EventArgs e)
        {
            OnClick?.Invoke(this, _comment);
        }
    }
}