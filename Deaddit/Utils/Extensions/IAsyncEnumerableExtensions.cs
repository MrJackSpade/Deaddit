namespace Deaddit.Utils.Extensions
{
    internal static class IAsyncEnumerableExtensions
    {
        public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> source) {
        
            List<T> toReturn = [];

            await foreach (T item in source)
            {

                toReturn.Add(item);
            }

            return toReturn;
        }
    }
}
