namespace Maui.WebComponents.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]
    public class HtmlEventAttribute(string? name = null, bool isGlobal = false) : Attribute
    {
        public bool IsGlobal { get; } = isGlobal;

        public string? Name { get; } = name;
    }
}