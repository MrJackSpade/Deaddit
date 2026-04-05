using Deaddit.Core.Configurations.Models;
using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using System.Reflection;

namespace Deaddit.Components.WebComponents.Forms
{
    [HtmlEntity("div")]
    public class ReadOnlyFieldComponent : FormFieldComponent
    {
        public ReadOnlyFieldComponent(string labelText, string? description, PropertyInfo property, object target, ApplicationStyling styling)
            : base(labelText, description, styling)
        {
            SpanComponent valueSpan = new()
            {
                InnerText = property.GetValue(target)?.ToString() ?? string.Empty,
                Color = styling.TextColor.ToHex(),
                Opacity = "0.7",
                Padding = "8px"
            };

            this.AddInput(valueSpan);
        }
    }
}
