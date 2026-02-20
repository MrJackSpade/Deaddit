using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using System.Reflection;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class EnumFieldComponent : FormFieldComponent
    {
        private readonly SelectComponent _select;
        private readonly PropertyInfo _property;
        private readonly object _target;
        private readonly Array _enumValues;

        public EnumFieldComponent(string labelText, string? description, PropertyInfo property, object target, ApplicationStyling styling)
            : base(labelText, description, styling)
        {
            _property = property;
            _target = target;
            _enumValues = Enum.GetValues(property.PropertyType);

            object? currentValue = property.GetValue(target);

            _select = new SelectComponent
            {
                Padding = "8px",
                Border = $"1px solid {styling.TertiaryColor.ToHex()}",
                BorderRadius = "4px",
                BackgroundColor = styling.TertiaryColor.ToHex(),
                Color = styling.TextColor.ToHex(),
                Width = "100%",
                BoxSizing = "border-box",
                Cursor = "pointer"
            };

            foreach (object enumValue in _enumValues)
            {
                OptionComponent option = new()
                {
                    Value = enumValue.ToString(),
                    InnerText = enumValue.ToString(),
                    Selected = enumValue.Equals(currentValue) ? "selected" : null
                };
                _select.Children.Add(option);
            }

            _select.OnChange += OnSelectionChanged;
            this.AddInput(_select);
        }

        private void OnSelectionChanged(object? sender, SelectChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Value))
            {
                object enumValue = Enum.Parse(_property.PropertyType, e.Value);
                _property.SetValue(_target, enumValue);
            }
        }
    }
}
