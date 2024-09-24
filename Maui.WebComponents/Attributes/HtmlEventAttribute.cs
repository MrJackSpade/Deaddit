namespace Maui.WebComponents.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HtmlEventAttribute(string? name = null) : Attribute
    {
        public string? Name { get; } = name;
    }
}