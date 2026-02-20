using Maui.WebComponents.Attributes;

namespace Maui.WebComponents.Components
{
    [HtmlEntity("option")]
    public class OptionComponent : WebComponent
    {
        [HtmlAttribute]
        public string? Value { get; set; }

        [HtmlAttribute]
        public string? Selected { get; set; }
    }
}
