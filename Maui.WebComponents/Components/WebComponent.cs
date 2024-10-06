using Maui.WebComponents.Attributes;
using Maui.WebComponents.Classes;
using Maui.WebComponents.Events;

namespace Maui.WebComponents.Components
{
    public partial class WebComponent
    {
        private string _innerHTML = string.Empty;

        private string _innerText = string.Empty;

        public WebComponentCollection Children { get; } = [];

        [HtmlAttribute]
        public string Id { get; } = "_" + Guid.NewGuid().ToString().Replace("-", "");

        public string InnerHTML
        {
            get => _innerHTML;
            set
            {
                if (_innerHTML != value)
                {
                    _innerHTML = value;
                    OnInnerHTMLChanged?.Invoke(this, new OnTextChangedEventArgs { Text = value });
                }
            }
        }

        public string InnerText
        {
            get => _innerText;
            set
            {
                if (_innerText != value)
                {
                    _innerText = value;
                    OnInnerTextChanged?.Invoke(this, new OnTextChangedEventArgs { Text = value });
                }
            }
        }

        public bool IsRendered { get; internal set; }

        public StyleCollection Style { get; } = [];

        [HtmlEvent("onclick", true)]
        public event EventHandler? OnClick;

        internal event EventHandler<OnTextChangedEventArgs>? OnInnerHTMLChanged;

        internal event EventHandler<OnTextChangedEventArgs>? OnInnerTextChanged;
    }
}