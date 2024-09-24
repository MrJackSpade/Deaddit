using Maui.WebComponents.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maui.WebComponents.Extensions
{
    public static class WebElementExtensions
    {
        public static async Task AddChild(this WebElement element, WebComponent child)
        {
            await element.InsertChild(-1, child);
        }
    }
}
