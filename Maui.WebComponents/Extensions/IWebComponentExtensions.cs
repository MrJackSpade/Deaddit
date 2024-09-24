using Maui.WebComponents.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maui.WebComponents.Extensions
{
    public static class IWebComponentExtensions
    {
        public static void Style(this IWebComponent component, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                component.Style.Remove(key);
                return;
            }

            component.Style.Add(key, value);    
        }

        public static string? Style(this IWebComponent component, string key)
        {
            if(component.Style.ContainsKey(key))
            {
                return component.Style[key];
            }

            return null;
        }
    }
}
