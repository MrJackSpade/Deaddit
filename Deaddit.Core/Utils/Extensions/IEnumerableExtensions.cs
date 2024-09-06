namespace Deaddit.Core.Utils.Extensions
{
    public static class IEnumerableExtensions
    {
        public static bool NotNullAny(this IEnumerable<object?> enumerable)
        {
            if (enumerable == null)
            {
                return false;
            }

            return enumerable.Any();
        }
    }
}