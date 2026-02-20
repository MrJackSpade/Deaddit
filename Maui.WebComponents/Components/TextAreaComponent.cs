using Maui.WebComponents.Attributes;
using Maui.WebComponents.Events;

namespace Maui.WebComponents.Components
{
    [HtmlEntity("textarea")]
    public class TextAreaComponent : WebComponent
    {
        [HtmlAttribute]
        public string? Rows { get; set; }

        [HtmlAttribute]
        public string? Cols { get; set; }

        [HtmlAttribute]
        public string? Placeholder { get; set; }

        [HtmlAttribute]
        public string? Name { get; set; }

        [HtmlEvent("oninput")]
        public event EventHandler<InputEventArgs>? OnInput;

        [HtmlEvent("onchange")]
        public event EventHandler<InputEventArgs>? OnChange;
    }
}
