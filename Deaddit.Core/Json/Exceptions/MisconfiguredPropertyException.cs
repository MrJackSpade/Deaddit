using System.Reflection;

namespace Deaddit.Core.Json.Exceptions
{
    public class MisconfiguredPropertyException(PropertyInfo propertyInfo, string message) : DeserializationException(message)
    {
        public PropertyInfo PropertyInfo { get; private set; } = propertyInfo;
    }
}