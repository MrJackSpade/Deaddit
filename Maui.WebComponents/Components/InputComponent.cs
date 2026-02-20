using Maui.WebComponents.Attributes;
using Maui.WebComponents.Events;

namespace Maui.WebComponents.Components
{
    [HtmlEntity("input")]
    public class InputComponent : WebComponent
    {
        [HtmlAttribute]
        public string? Type { get; set; } = "text";

        [HtmlAttribute]
        public string? Value { get; set; }

        [HtmlAttribute]
        public string? Placeholder { get; set; }

        [HtmlAttribute]
        public string? Checked { get; set; }

        [HtmlAttribute]
        public string? Name { get; set; }

        [HtmlEvent("oninput")]
        public event EventHandler<InputEventArgs>? OnInput;

        [HtmlEvent("onchange")]
        public event EventHandler<InputEventArgs>? OnChange;
    }
}
