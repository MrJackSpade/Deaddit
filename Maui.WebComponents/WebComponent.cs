using Maui.WebComponents.Attributes;
using Maui.WebComponents.Interfaces;

namespace Maui.WebComponents
{
    public class WebComponent : IWebComponent
    {
        public event EventHandler? OnClick;

        public List<IWebComponent> Children { get; } = [];

        public string Id { get; } = Guid.NewGuid().ToString();

        public string InnerText { get; set; }

        public Dictionary<string, string> Style { get; } = [];

        [HtmlEvent("onclick")]
        public void Click(object sender, EventArgs args)
        {
            OnClick?.Invoke(this, EventArgs.Empty);
        }
    }
}