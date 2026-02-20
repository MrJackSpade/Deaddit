using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using Reddit.Api.Models;
using System.Reflection;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class ColorFieldComponent : FormFieldComponent
    {
        private readonly InputComponent _input;
        private readonly DivComponent _colorPreview;
        private readonly SpanComponent _errorLabel;
        private readonly PropertyInfo _property;
        private readonly object _target;
        private readonly ApplicationStyling _styling;

        public ColorFieldComponent(string labelText, string? description, PropertyInfo property, object target, ApplicationStyling styling)
            : base(labelText, description, styling)
        {
            _property = property;
            _target = target;
            _styling = styling;

            DynamicColor? currentColor = property.GetValue(target) as DynamicColor;
            string? hexValue = currentColor?.ToHex();

            DivComponent container = new()
            {
                Display = "flex",
                AlignItems = "center",
                Width = "100%"
            };

            _colorPreview = new DivComponent
            {
                Width = "36px",
                Height = "36px",
                BorderRadius = "4px",
                Border = $"1px solid {styling.TertiaryColor.ToHex()}",
                BackgroundColor = hexValue ?? "#000000",
                MarginRight = "8px",
                FlexShrink = "0"
            };

            _input = new InputComponent
            {
                Type = "text",
                Value = hexValue ?? string.Empty,
                Placeholder = "#RRGGBB",
                Padding = "8px",
                Border = $"1px solid {styling.TertiaryColor.ToHex()}",
                BorderRadius = "4px",
                BackgroundColor = styling.TertiaryColor.ToHex(),
                Color = styling.TextColor.ToHex(),
                FlexGrow = "1",
                BoxSizing = "border-box"
            };

            _errorLabel = new SpanComponent
            {
                Color = "#ff4444",
                FontSize = "12px",
                Display = "none",
                MarginTop = "4px"
            };

            _input.OnInput += OnInputChanged;

            container.Children.Add(_colorPreview);
            container.Children.Add(_input);

            this.AddInput(container);
            this.AddInput(_errorLabel);
        }

        private void OnInputChanged(object? sender, InputEventArgs e)
        {
            string? value = e.Value;

            if (DynamicColor.TryParse(value, out DynamicColor colorValue))
            {
                _property.SetValue(_target, colorValue);
                _colorPreview.BackgroundColor = colorValue.ToHex();
                _errorLabel.Display = "none";
                _input.Border = $"1px solid {_styling.TertiaryColor.ToHex()}";
            }
            else
            {
                _errorLabel.InnerText = "Invalid color format (use #RRGGBB or #RRGGBBAA)";
                _errorLabel.Display = "block";
                _input.Border = "1px solid #ff4444";
            }
        }
    }
}
