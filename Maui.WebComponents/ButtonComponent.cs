using Maui.WebComponents.Attributes;
using Maui.WebComponents.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maui.WebComponents
{
    [HtmlEntity("button")]
    public class ButtonComponent : WebComponent
    {
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

        public string? BoxShadow
        {
            get => this.Style("box-shadow");
            set => this.Style("box-shadow", value);
        }

        public string? Color
        {
            get => this.Style("color");
            set => this.Style("color", value);
        }

        public string? Cursor
        {
            get => this.Style("cursor");
            set => this.Style("cursor", value);
        }

        public string? Display
        {
            get => this.Style("display");
            set => this.Style("display", value);
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

        public string? TextAlign
        {
            get => this.Style("text-align");
            set => this.Style("text-align", value);
        }

        public string? Width
        {
            get => this.Style("width");
            set => this.Style("width", value);
        }
    }
}