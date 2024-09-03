namespace Deaddit.Extensions
{
    internal static class SemaphoreSlimExtensions
    {
        public static void Wait(this SemaphoreSlim semaphore, Action action)
        {
            semaphore.Wait();
            action();
            semaphore.Release();
        }
    }
}
