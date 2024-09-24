using Maui.WebComponents.Attributes;
using Maui.WebComponents.Extensions;

namespace Maui.WebComponents
{
    // Example usage remains the same
    [HtmlEntity("span")]
    public class SpanComponent : WebComponent
    {
        public string? Color
        {
            get => this.Style("color");
            set => this.Style("color", value);
        }

        public string? FontSize
        {
            get => this.Style("font-size");
            set => this.Style("font-size", value);
        }

        public string? FontWeight
        {
            get => this.Style("font-weight");
            set => this.Style("font-weight", value);
        }

        public string? TextAlign
        {
            get => this.Style("text-align");
            set => this.Style("text-align", value);
        }

        public string? BackgroundColor
        {
            get => this.Style("background-color");
            set => this.Style("background-color", value);
        }

        public string? LineHeight
        {
            get => this.Style("line-height");
            set => this.Style("line-height", value);
        }

        public string? Margin
        {
            get => this.Style("margin");
            set => this.Style("margin", value);
        }

        public string? Padding
        {
            get => this.Style("padding");
            set => this.Style("padding", value);
        }

        public string? Border
        {
            get => this.Style("border");
            set => this.Style("border", value);
        }

        public string? Display
        {
            get => this.Style("display");
            set => this.Style("display", value);
        }
    }
}