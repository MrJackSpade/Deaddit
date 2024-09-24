using Maui.WebComponents.Attributes;
using Maui.WebComponents.Classes;
using Maui.WebComponents.Events;

namespace Maui.WebComponents.Components
{
    public class WebComponent
    {
        private string _innerText = string.Empty;

        public event EventHandler? OnClick;

        internal event EventHandler<OnInnerTextChangedEventArgs>? OnInnerTextChanged;

        public List<WebComponent> Children { get; } = [];

        [HtmlAttribute]
        public string Id { get; } = Guid.NewGuid().ToString();

        public string InnerText
        {
            get => _innerText;
            set
            {
                if (_innerText != value)
                {
                    _innerText = value;
                    OnInnerTextChanged?.Invoke(this, new OnInnerTextChangedEventArgs { InnerText = value });
                }
            }
        }

        public StyleCollection Style { get; } = [];

        [HtmlEvent("onclick")]
        public void Click(object sender, EventArgs args)
        {
            OnClick?.Invoke(this, EventArgs.Empty);
        }
    }
}