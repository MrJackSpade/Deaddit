﻿using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils.Extensions;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents
{
    [HtmlEntity("reddit-more-comments")]
    public class MoreCommentsWebComponent : DivComponent, IMore
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly IMore _more;

        private readonly bool _singleClick;

        private bool _clicked;

        public List<string> ChildNames { get; } = [];

        public int? Count { get; }

        public ApiThing Parent => _more.Parent;

        public event EventHandler<IMore>? LoadMore;

        public MoreCommentsWebComponent(IMore more, ApplicationStyling applicationStyling)
        {
            _more = more;
            _applicationStyling = applicationStyling;

            bool isContinueThread = !more.ChildNames.NotNullAny();

            _singleClick = !isContinueThread;

            string display = !isContinueThread ? $"More {more.Count}" : "Continue Thread";

            if (more is CollapsedMore cm)
            {
                display = cm.CollapsedReasonCode switch
                {
                    CollapsedReasonKind.Deleted => "Deleted",
                    CollapsedReasonKind.LowScore => "Low Score",
                    CollapsedReasonKind.BlockedAuthor => "Blocked Author",
                    _ => cm.CollapsedReasonCode.ToString()
                };
            }

            InnerText = display;
            BackgroundColor = _applicationStyling.PrimaryColor.ToHex();
            Width = "100%";
            Color = _applicationStyling.TextColor.ToHex();
            Display = "block";
            FontSize = $"{(int)(applicationStyling.TitleFontSize * .90)}px";
            PaddingLeft = "5px";
            PaddingTop = "5px";
            PaddingBottom = "5px";
            OnClick += this.Clicked;
        }

        public void Clicked(object? sender, EventArgs e)
        {
            if (_clicked && _singleClick)
            {
                return;
            }

            _clicked = true;

            LoadMore?.Invoke(this, _more);
        }
    }
}