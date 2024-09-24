using Maui.WebComponents.Components;

namespace Maui.WebComponents.Extensions
{
    public static class IWebComponentExtensions
    {
        public static void Style(this WebComponent component, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                component.Style.Remove(key);
                return;
            }
            else if (component.Style.ContainsKey(key))
            {
                component.Style[key] = value;
                return;
            }
            else
            {
                component.Style.Add(key, value);
            }
        }

        public static string? Style(this WebComponent component, string key)
        {
            if (component.Style.ContainsKey(key))
            {
                return component.Style[key];
            }

            return null;
        }
    }
}