namespace Deaddit.Attributes
{
    public class InputAttribute : Attribute
    {
        public InputAttribute(bool masked)
        {
            Masked = masked;
        }

        public bool Masked { get; private set; }
    }
}