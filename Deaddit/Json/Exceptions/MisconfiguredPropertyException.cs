using System.Reflection;

namespace Deaddit.Json.Exceptions
{
    internal class MisconfiguredPropertyException(PropertyInfo propertyInfo, string message) : DeserializationException(message)
    {
        public PropertyInfo PropertyInfo { get; private set; } = propertyInfo;
    }
}