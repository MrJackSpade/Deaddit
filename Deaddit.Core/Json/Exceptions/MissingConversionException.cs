namespace Deaddit.Core.Json.Exceptions
{
    public class MissingConversionException : DeserializationException
    {
        public MissingConversionException()
        {
        }

        public MissingConversionException(string? message) : base(message)
        {
        }

        public MissingConversionException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}