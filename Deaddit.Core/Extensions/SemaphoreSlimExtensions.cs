namespace Deaddit.Core.Extensions
{
    public static class SemaphoreSlimExtensions
    {
        public static void Try(this SemaphoreSlim semaphore, Action action)
        {
            if (semaphore.Wait(0))
            {
                try
                {
                    action();
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        public static void Wait(this SemaphoreSlim semaphore, Action action)
        {
            semaphore.Wait();
            action();
            semaphore.Release();
        }
    }
}