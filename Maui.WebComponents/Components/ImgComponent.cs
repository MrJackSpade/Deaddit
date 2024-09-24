using Maui.WebComponents.Attributes;
using Maui.WebComponents.Extensions;

namespace Maui.WebComponents.Components
{
    // Example usage remains the same
    [HtmlEntity("img")]
    public class ImgComponent : WebComponent
    {
        public string? Border
        {
            get => this.Style("border");
            set => this.Style("border", value);
        }

        public string? BorderRadius
        {
            get => this.Style("border-radius");
            set => this.Style("border-radius", value);
        }

        public string? BoxShadow
        {
            get => this.Style("box-shadow");
            set => this.Style("box-shadow", value);
        }

        public string? Height
        {
            get => this.Style("height");
            set => this.Style("height", value);
        }

        public string? Margin
        {
            get => this.Style("margin");
            set => this.Style("margin", value);
        }

        public string? MaxHeight
        {
            get => this.Style("max-height");
            set => this.Style("max-height", value);
        }

        public string? MaxWidth
        {
            get => this.Style("max-width");
            set => this.Style("max-width", value);
        }

        public string? MinHeight
        {
            get => this.Style("min-height");
            set => this.Style("min-height", value);
        }

        public string? MinWidth
        {
            get => this.Style("min-width");
            set => this.Style("min-width", value);
        }

        public string? ObjectFit
        {
            get => this.Style("object-fit");
            set => this.Style("object-fit", value);
        }

        public string? ObjectPosition
        {
            get => this.Style("object-position");
            set => this.Style("object-position", value);
        }

        public string? Opacity
        {
            get => this.Style("opacity");
            set => this.Style("opacity", value);
        }

        public string? Padding
        {
            get => this.Style("padding");
            set => this.Style("padding", value);
        }

        [HtmlAttribute]
        public string? Src { get; set; }

        public string? Width
        {
            get => this.Style("width");
            set => this.Style("width", value);
        }
    }
}