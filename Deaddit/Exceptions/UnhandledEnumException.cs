namespace Deaddit.Exceptions
{
    public class UnhandledEnumException(Enum value) : ArgumentException($"Enum of type '{value.GetType()}' with value '{value}' is unhandled")
    {
        public Enum Enum { get; set; } = value;
    }
}