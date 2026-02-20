using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using System.Reflection;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class NumericFieldComponent : FormFieldComponent
    {
        private readonly InputComponent _input;
        private readonly SpanComponent _errorLabel;
        private readonly PropertyInfo _property;
        private readonly object _target;
        private readonly Type _numericType;
        private readonly ApplicationStyling _styling;

        public NumericFieldComponent(string labelText, string? description, PropertyInfo property, object target, ApplicationStyling styling)
            : base(labelText, description, styling)
        {
            _property = property;
            _target = target;
            _numericType = property.PropertyType;
            _styling = styling;

            _input = new InputComponent
            {
                Type = "number",
                Value = property.GetValue(target)?.ToString() ?? "0",
                Padding = "8px",
                Border = $"1px solid {styling.TertiaryColor.ToHex()}",
                BorderRadius = "4px",
                BackgroundColor = styling.TertiaryColor.ToHex(),
                Color = styling.TextColor.ToHex(),
                Width = "100%",
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
            this.AddInput(_input);
            this.AddInput(_errorLabel);
        }

        private void OnInputChanged(object? sender, InputEventArgs e)
        {
            string? value = e.Value;

            if (_numericType == typeof(int))
            {
                if (int.TryParse(value, out int intValue))
                {
                    _property.SetValue(_target, intValue);
                    _errorLabel.Display = "none";
                    _input.Border = $"1px solid {_styling.TertiaryColor.ToHex()}";
                }
                else
                {
                    _errorLabel.InnerText = "Invalid integer value";
                    _errorLabel.Display = "block";
                    _input.Border = "1px solid #ff4444";
                }
            }
            else if (_numericType == typeof(double))
            {
                if (double.TryParse(value, out double doubleValue))
                {
                    _property.SetValue(_target, doubleValue);
                    _errorLabel.Display = "none";
                    _input.Border = $"1px solid {_styling.TertiaryColor.ToHex()}";
                }
                else
                {
                    _errorLabel.InnerText = "Invalid decimal value";
                    _errorLabel.Display = "block";
                    _input.Border = "1px solid #ff4444";
                }
            }
        }
    }
}
