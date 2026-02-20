using Maui.WebComponents.Attributes;

namespace Maui.WebComponents.Components
{
    [HtmlEntity("label")]
    public class LabelComponent : WebComponent
    {
        [HtmlAttribute]
        public string? For { get; set; }
    }
}
