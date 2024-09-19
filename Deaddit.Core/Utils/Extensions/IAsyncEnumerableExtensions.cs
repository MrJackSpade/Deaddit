namespace Deaddit.Core.Utils.Extensions
{
    public static class IAsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<T> Take<T>(this Task<List<T>> source, int count)
        {
            int taken = 0;

            foreach (T item in await source)
            {
                yield return item;

                taken++;

                if (taken >= count)
                {
                    break;
                }
            }
        }

        public static async IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, int count)
        {
            int taken = 0;

            await foreach (T item in source)
            {
                yield return item;

                taken++;

                if (taken >= count)
                {
                    break;
                }
            }
        }

        public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> source)
        {
            List<T> toReturn = [];

            await foreach (T item in source)
            {
                toReturn.Add(item);
            }

            return toReturn;
        }
    }
}