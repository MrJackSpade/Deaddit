using Maui.WebComponents.Attributes;

namespace Maui.WebComponents.Components
{
    // Example usage remains the same
    [HtmlEntity("img")]
    public class ImgComponent : WebComponent
    {
        [HtmlAttribute]
        public string? Src { get; set; }
    }
}