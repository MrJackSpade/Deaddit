using Maui.WebComponents.Attributes;
using Maui.WebComponents.Events;

namespace Maui.WebComponents.Components
{
    [HtmlEntity("select")]
    public class SelectComponent : WebComponent
    {
        [HtmlEvent("onchange")]
        public event EventHandler<SelectChangedEventArgs>? OnChange;

        [HtmlAttribute]
        public string? Name { get; set; }
    }
}