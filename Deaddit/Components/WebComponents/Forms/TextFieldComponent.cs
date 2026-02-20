using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using System.Reflection;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class TextFieldComponent : FormFieldComponent
    {
        private readonly InputComponent _input;
        private readonly PropertyInfo _property;
        private readonly object _target;

        public TextFieldComponent(string labelText, string? description, bool masked, PropertyInfo property, object target, ApplicationStyling styling)
            : base(labelText, description, styling)
        {
            _property = property;
            _target = target;

            _input = new InputComponent
            {
                Type = masked ? "password" : "text",
                Value = property.GetValue(target)?.ToString() ?? string.Empty,
                Padding = "8px",
                Border = $"1px solid {styling.TertiaryColor.ToHex()}",
                BorderRadius = "4px",
                BackgroundColor = styling.TertiaryColor.ToHex(),
                Color = styling.TextColor.ToHex(),
                Width = "100%",
                BoxSizing = "border-box"
            };

            _input.OnInput += OnInputChanged;
            this.AddInput(_input);
        }

        private void OnInputChanged(object? sender, InputEventArgs e)
        {
            _property.SetValue(_target, e.Value);
        }
    }
}
