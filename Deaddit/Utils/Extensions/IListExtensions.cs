namespace Deaddit.Utils.Extensions
{
    internal static class IListExtensions
    {
        public static void InsertAfter<T>(this IList<T> source, T before, T toInsert)
        {
            int elementIndex = source.IndexOf(before);

            if (elementIndex < 0)
            {
                throw new ArgumentException("Element does not exist in target");
            }

            if (elementIndex == source.Count - 1)
            {
                source.Add(toInsert);
            }
            else
            {
                source.Insert(elementIndex + 1, toInsert);
            }
        }

        public static void InsertBefore<T>(this IList<T> source, T before, T toInsert)
        {
            int elementIndex = source.IndexOf(before);

            if (elementIndex < 0)
            {
                throw new ArgumentException("Element does not exist in target");
            }

            source.Insert(elementIndex, toInsert);
        }
    }
}