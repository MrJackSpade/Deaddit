namespace Maui.WebComponents.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class HtmlEntityAttribute : Attribute
    {
        public string Tag { get; }

        public HtmlEntityAttribute(string tag)
        {
            Tag = tag;
        }
    }
}