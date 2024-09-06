namespace Deaddit.Core.Json.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class JsonPropertyNamesAttribute(params string[] names) : Attribute
    {
        public string[] Names { get; set; } = names ?? [];
    }
}