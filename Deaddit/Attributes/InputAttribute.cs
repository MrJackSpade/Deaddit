namespace Deaddit.Attributes
{
    public class InputAttribute : Attribute
    {
        public bool Masked { get; set; }

        public string? Name { get; set; }

        public int Order { get; set; }
    }
}