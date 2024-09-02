using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
