using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;

namespace Deaddit.Components.WebComponents.Partials
{
    [HtmlEntity("spinner")]
    internal class Spinner : WebComponent
    {
        public Spinner(string color)
        {
            DivComponent component = new()
            {
                BorderBottomColor = color
            };

            Children.Add(component);
        }
    }
}