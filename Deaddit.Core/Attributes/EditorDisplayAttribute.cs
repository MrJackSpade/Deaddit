namespace Deaddit.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EditorDisplayAttribute : Attribute
    {
        public string? Description { get; set; }

        public bool Masked { get; set; }

        public bool Multiline { get; set; }

        public string? Name { get; set; }

        public int Order { get; set; }
    }
}