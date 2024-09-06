namespace Deaddit.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EditorDisplayAttribute : Attribute
    {
        public bool Masked { get; set; }

        public string? Name { get; set; }

        public int Order { get; set; }
    }
}