using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using Maui.WebComponents.Events;
using System.Reflection;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class BooleanFieldComponent : FormFieldComponent
    {
        private readonly InputComponent _checkbox;
        private readonly PropertyInfo _property;
        private readonly object _target;

        public BooleanFieldComponent(string labelText, string? description, PropertyInfo property, object target, ApplicationStyling styling)
            : base(labelText, description, styling)
        {
            _property = property;
            _target = target;

            bool isChecked = (bool)(property.GetValue(target) ?? false);

            DivComponent container = new()
            {
                Display = "flex",
                AlignItems = "center"
            };

            _checkbox = new InputComponent
            {
                Type = "checkbox",
                Checked = isChecked ? "checked" : null,
                Width = "20px",
                Height = "20px",
                Cursor = "pointer"
            };

            _checkbox.OnChange += OnCheckboxChanged;
            container.Children.Add(_checkbox);

            this.AddInput(container);
        }

        private void OnCheckboxChanged(object? sender, InputEventArgs e)
        {
            _property.SetValue(_target, e.Checked ?? false);
        }
    }
}
