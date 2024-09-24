using Maui.WebComponents.Components;

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
