using Deaddit.Components.WebComponents.Partials;
using Deaddit.Core.Extensions;
using Deaddit.Core.Utils;

using Maui.WebComponents;
using Maui.WebComponents.Components;
using Maui.WebComponents.Extensions;
using System.Diagnostics;

namespace Deaddit.Utils
{
    internal static class DataService
    {
        private static readonly SemaphoreSlim _semaphore = new(1);

        private static readonly List<Task> _tasks = [];

        public static async Task LoadAsync(Func<Task> toExecute)
        {
            Task executing = toExecute();

            Debug.WriteLine("--Starting async Task");

            _semaphore.Wait(() => _tasks.Add(executing));

            await executing;

            Debug.WriteLine("--Completed async Task");

            _semaphore.Wait(() => _tasks.Remove(executing));
        }

        public static async Task LoadAsync(WebElement activityContainer, Func<Task> toExecute, string indicatorColor, bool top = false)
        {
            Ensure.NotNull(activityContainer);

            WebComponent activityIndicator = new Spinner(indicatorColor);

            if (top)
            {
                activityContainer?.InsertChild(0, activityIndicator);
            }
            else
            {
                activityContainer?.AddChild(activityIndicator);
            }

            await LoadAsync(toExecute);

            activityContainer?.RemoveChild(activityIndicator);
        }

        public static async Task LoadAsync(WebComponent activityContainer, Func<Task> toExecute, string indicatorColor, bool top = false)
        {
            Ensure.NotNull(activityContainer);

            WebComponent activityIndicator = new Spinner(indicatorColor);

            if (top)
            {
                activityContainer?.Children?.Insert(0, activityIndicator);
            }
            else
            {
                activityContainer?.Children?.Add(activityIndicator);
            }

            await LoadAsync(toExecute);

            activityContainer?.Children?.Remove(activityIndicator);
        }
    }
}