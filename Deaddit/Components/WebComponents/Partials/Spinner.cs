using Maui.WebComponents.Attributes;
using Maui.WebComponents.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Components.WebComponents.Partials
{
    [HtmlEntity("spinner")]
    internal class Spinner : WebComponent
    {
        public Spinner(string color)
        {
            DivComponent component = new();

            component.BorderBottomColor = color;

            this.Children.Add(component);
        }
    }
}
