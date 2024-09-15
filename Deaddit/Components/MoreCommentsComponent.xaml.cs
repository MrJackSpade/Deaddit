using Deaddit.Components.ComponentModels;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils.Extensions;

namespace Deaddit.MAUI.Components
{
    public partial class MoreCommentsComponent : ContentView
    {
        private readonly IMore _comment;

        private readonly bool _singleClick;

        private bool _clicked;

        public MoreCommentsComponent(IMore comment, ApplicationStyling applicationTheme)
        {
            bool isContinueThread = !comment.ChildNames.NotNullAny();
            _singleClick = !isContinueThread;

            string display = !isContinueThread ? $"More {comment.Count}" : "Continue Thread";

            if (comment is CollapsedMore cm)
            {
                display = cm.CollapsedReasonCode switch
                {
                    CollapsedReasonKind.Deleted => "Deleted",
                    CollapsedReasonKind.LowScore => "Low Score",
                    _ => cm.CollapsedReasonCode.ToString()
                };
            }

            _comment = comment;
            BindingContext = new MoreCommentsComponentViewModel(display, applicationTheme);
            this.InitializeComponent();
        }

        public event EventHandler<IMore>? OnClick;

        public void OnParentTapped(object? sender, EventArgs e)
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