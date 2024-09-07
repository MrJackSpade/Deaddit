using Deaddit.Components.ComponentModels;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils.Extensions;

namespace Deaddit.MAUI.Components
{
    public partial class MoreCommentsComponent : ContentView
    {
        private readonly ApplicationStyling _applicationTheme;

        private readonly ApiMore _comment;

        private readonly MoreCommentsComponentViewModel _commentViewModel;

        private readonly bool _singleClick;

        private bool _clicked;

        public MoreCommentsComponent(ApiMore comment, ApplicationStyling applicationTheme)
        {
            bool isContinueThread = !comment.ChildNames.NotNullAny();
            _singleClick = !isContinueThread;
            string display = !isContinueThread ? $"More {comment.Count}" : "Continue Thread";

            _applicationTheme = applicationTheme;
            _comment = comment;
            BindingContext = _commentViewModel = new MoreCommentsComponentViewModel(display, applicationTheme);
            this.InitializeComponent();
        }

        public event EventHandler<ApiMore>? OnClick;

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