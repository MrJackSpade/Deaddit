using Maui.WebComponents.Classes;

namespace Maui.WebComponents.Events
{
    internal class OnStyleRuleAddedEventArgs(StyleRule styleRule)
    {
        public StyleRule StyleRule { get; set; } = styleRule;
    }
}