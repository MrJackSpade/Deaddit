namespace Deaddit.Attributes
{
    internal class JsonPropertyNamesAttribute : Attribute
    {
        public JsonPropertyNamesAttribute(params string[] names)
        {
            Names = names ?? [];
        }

        public string[] Names { get; set; } = [];
    }
}