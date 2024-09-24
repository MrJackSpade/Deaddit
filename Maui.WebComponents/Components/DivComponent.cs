using Maui.WebComponents.Attributes;
using Maui.WebComponents.Extensions;

namespace Maui.WebComponents.Components
{
    [HtmlEntity("div")]
    public class DivComponent : WebComponent
    {
        public string? AlignItems
        {
            get => this.Style("align-items");
            set => this.Style("align-items", value);
        }

        public string? BackgroundColor
        {
            get => this.Style("background-color");
            set => this.Style("background-color", value);
        }

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

        public string? Bottom
        {
            get => this.Style("bottom");
            set => this.Style("bottom", value);
        }

        public string? BoxShadow
        {
            get => this.Style("box-shadow");
            set => this.Style("box-shadow", value);
        }

        public string? Display
        {
            get => this.Style("display");
            set => this.Style("display", value);
        }

        public string? FlexDirection
        {
            get => this.Style("flex-direction");
            set => this.Style("flex-direction", value);
        }

        public string? FlexGrow
        {
            get => this.Style("flex-grow");
            set => this.Style("flex-grow", value);
        }

        public string? Height
        {
            get => this.Style("height");
            set => this.Style("height", value);
        }

        public string? JustifyContent
        {
            get => this.Style("justify-content");
            set => this.Style("justify-content", value);
        }

        public string? Left
        {
            get => this.Style("left");
            set => this.Style("left", value);
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

        public string? Opacity
        {
            get => this.Style("opacity");
            set => this.Style("opacity", value);
        }

        public string? Overflow
        {
            get => this.Style("overflow");
            set => this.Style("overflow", value);
        }

        public string? Padding
        {
            get => this.Style("padding");
            set => this.Style("padding", value);
        }

        public string? Position
        {
            get => this.Style("position");
            set => this.Style("position", value);
        }

        public string? Right
        {
            get => this.Style("right");
            set => this.Style("right", value);
        }

        public string? TextAlign
        {
            get => this.Style("text-align");
            set => this.Style("text-align", value);
        }

        public string? Top
        {
            get => this.Style("top");
            set => this.Style("top", value);
        }

        public string? Width
        {
            get => this.Style("width");
            set => this.Style("width", value);
        }

        public string? ZIndex
        {
            get => this.Style("z-index");
            set => this.Style("z-index", value);
        }
    }
}