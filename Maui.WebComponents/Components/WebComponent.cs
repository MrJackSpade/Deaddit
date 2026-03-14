using Maui.WebComponents.Attributes;
using Maui.WebComponents.Classes;
using Maui.WebComponents.Events;

namespace Maui.WebComponents.Components
{
    public partial class WebComponent
    {
        [HtmlEvent("onclick", true)]
        public event EventHandler? OnClick;

        internal event EventHandler<OnTextChangedEventArgs>? OnInnerHTMLChanged;

        internal event EventHandler<OnTextChangedEventArgs>? OnInnerTextChanged;

        public WebComponentCollection Children { get; } = [];

        [HtmlAttribute]
        public string Id { get; } = "_" + Guid.NewGuid().ToString().Replace("-", "");

        public string InnerHTML
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnInnerHTMLChanged?.Invoke(this, new OnTextChangedEventArgs { Text = value });
                }
            }
        } = string.Empty;

        public string InnerText
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnInnerTextChanged?.Invoke(this, new OnTextChangedEventArgs { Text = value });
                }
            }
        } = string.Empty;

        public bool IsRendered { get; internal set; }

        public StyleCollection Style { get; } = [];
    }
}