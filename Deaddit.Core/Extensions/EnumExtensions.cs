using System.Reflection;

namespace Deaddit.Core.Extensions
{
    public static class EnumExtensions
    {
        public static TAttribute GetAttribute<TAttribute>(this Enum enumValue) where TAttribute : Attribute
        {
            // Get the type of the enum
            Type type = enumValue.GetType();

            // Get the member information for the specific enum value
            MemberInfo[] memberInfo = type.GetMember(enumValue.ToString());

            // Check if the member information is not null and has at least one member
            if (memberInfo.Length > 0)
            {
                // Get the custom attribute of the specified type
                TAttribute? attribute = memberInfo[0].GetCustomAttribute<TAttribute>();

                // Return the attribute (it will be null if the attribute is not found)
                return attribute;
            }

            return null;
        }
    }
}