using System.Reflection;
using System.Runtime.CompilerServices;

namespace Deaddit.Core.Utils.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool HasCustomAttribute<T>(this PropertyInfo propertyInfo) where T : Attribute
        {
            return propertyInfo.GetCustomAttributes(typeof(T), true).Any();
        }

        public static bool IsRequired(this PropertyInfo propertyInfo)
        {
            return propertyInfo.HasCustomAttribute<RequiredMemberAttribute>();
        }
    }
}