using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using System.Reflection;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class MultilineTextFieldComponent : FormFieldComponent
    {
        private readonly TextAreaComponent _textArea;
        private readonly PropertyInfo _property;
        private readonly object _target;

        public MultilineTextFieldComponent(string labelText, string? description, PropertyInfo property, object target, ApplicationStyling styling)
            : base(labelText, description, styling)
        {
            _property = property;
            _target = target;

            _textArea = new TextAreaComponent
            {
                Rows = "4",
                Padding = "8px",
                Border = $"1px solid {styling.TertiaryColor.ToHex()}",
                BorderRadius = "4px",
                BackgroundColor = styling.TertiaryColor.ToHex(),
                Color = styling.TextColor.ToHex(),
                Width = "100%",
                BoxSizing = "border-box",
                InnerText = property.GetValue(target)?.ToString() ?? string.Empty
            };

            _textArea.OnInput += OnInputChanged;
            this.AddInput(_textArea);
        }

        private void OnInputChanged(object? sender, InputEventArgs e)
        {
            _property.SetValue(_target, e.Value);
        }
    }
}
