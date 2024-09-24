namespace Maui.WebComponents.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class HtmlEntityAttribute : Attribute
    {
        public HtmlEntityAttribute(string tag)
        {
            Tag = tag;
        }

        public string Tag { get; }
    }
}