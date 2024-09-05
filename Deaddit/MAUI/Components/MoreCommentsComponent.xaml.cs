using Deaddit.Configurations.Models;
using Deaddit.MAUI.Components.ComponentModels;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils.Extensions;

namespace Deaddit.MAUI.Components
{
    public partial class MoreCommentsComponent : ContentView
    {
        private readonly ApplicationTheme _applicationTheme;

        private readonly ApiComment _comment;

        private readonly MoreCommentsComponentViewModel _commentViewModel;

        private bool _clicked;

        private readonly bool _singleClick;

        public MoreCommentsComponent(ApiComment comment, ApplicationTheme applicationTheme)
        {
            bool isContinueThread = !comment.ChildNames.NotNullAny();
            _singleClick = !isContinueThread;
            string display = !isContinueThread ? $"More {comment.Count}" : "Continue Thread";

            _applicationTheme = applicationTheme;
            _comment = comment;
            BindingContext = _commentViewModel = new MoreCommentsComponentViewModel(display, applicationTheme);
            this.InitializeComponent();
        }

        public event EventHandler<ApiComment>? OnClick;

        public async void OnParentTapped(object? sender, EventArgs e)
        {
            if (_clicked && _singleClick)
            {
                return;
            }

            _clicked = true;

            OnClick?.Invoke(this, _comment);
        }
    }
}