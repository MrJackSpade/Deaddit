using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maui.WebComponents.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HtmlEventAttribute(string? name = null) : Attribute
    {
        public string? Name { get; } = name;
    }
}