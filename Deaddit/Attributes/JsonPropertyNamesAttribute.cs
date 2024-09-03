namespace Deaddit.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class JsonPropertyNamesAttribute(params string[] names) : Attribute
    {
        public string[] Names { get; set; } = names ?? [];
    }
}