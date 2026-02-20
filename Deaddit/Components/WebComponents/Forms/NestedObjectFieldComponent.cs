using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using System.Reflection;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class NestedObjectFieldComponent : FormFieldComponent
    {
        private readonly ButtonComponent _editButton;
        private readonly PropertyInfo _property;
        private readonly object _target;

        public event EventHandler<object>? OnEditClicked;

        public NestedObjectFieldComponent(string labelText, string? description, PropertyInfo property, object target, ApplicationStyling styling)
            : base(labelText, description, styling)
        {
            _property = property;
            _target = target;

            _editButton = new ButtonComponent
            {
                InnerText = "Edit",
                Padding = "8px 16px",
                Border = "none",
                BorderRadius = "4px",
                BackgroundColor = styling.PrimaryColor.ToHex(),
                Color = styling.TextColor.ToHex(),
                Cursor = "pointer"
            };

            _editButton.OnClick += OnEditButtonClicked;
            this.AddInput(_editButton);
        }

        private void OnEditButtonClicked(object? sender, EventArgs e)
        {
            object? nestedObj = _property.GetValue(_target);
            if (nestedObj != null)
            {
                OnEditClicked?.Invoke(this, nestedObj);
            }
        }
    }
}
