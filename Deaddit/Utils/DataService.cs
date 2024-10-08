﻿using Deaddit.Core.Extensions;
using System.Diagnostics;

namespace Deaddit.Utils
{
    internal static class DataService
    {
        private static readonly SemaphoreSlim _semaphore = new(1);

        private static readonly List<Task> _tasks = [];

        public static async Task LoadAsync(Layout activityContainer, Func<Task> toExecute, Color? indicatorColor = null, bool top = false)
        {
            ActivityIndicator activityIndicator = new()
            {
                IsRunning = true,
                Color = indicatorColor,
            };

            if (top)
            {
                activityContainer.Children.Insert(0, activityIndicator);
            }
            else
            {
                activityContainer.Children.Add(activityIndicator);
            }

            Task executing = toExecute();

            Debug.WriteLine("--Starting async Task");

            _semaphore.Wait(() => _tasks.Add(executing));

            await executing;

            Debug.WriteLine("--Completed async Task");

            _semaphore.Wait(() => _tasks.Remove(executing));

            activityContainer.Children.Remove(activityIndicator);
        }
    }
}