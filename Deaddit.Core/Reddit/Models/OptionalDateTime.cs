namespace Deaddit.Core.Reddit.Models
{
    public struct OptionalDateTime
    {
        public static OptionalDateTime Null => new();

        public readonly bool HasValue => Value != null;

        public DateTime? Value { get; set; }

        public static implicit operator OptionalDateTime(DateTime source)
        {
            return new OptionalDateTime() { Value = source };
        }

        public override string ToString()
        {
            return Value?.ToShortDateString() ?? string.Empty;
        }
    }
}