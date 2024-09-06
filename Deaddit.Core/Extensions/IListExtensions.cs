namespace Deaddit.Core.Extensions
{
    public static class IListExtensions
    {
        public static void Add<T>(this IList<T> target, T item1, T item2, params T[] other)
        {
            target.Add(item1);
            target.Add(item2);

            if (other != null)
            {
                foreach (T? otherItem in other)
                {
                    target.Add(otherItem);
                }
            }
        }
    }
}