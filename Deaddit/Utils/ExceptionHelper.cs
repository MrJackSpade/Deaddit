using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Deaddit.Utils
{
    internal static class ExceptionHelper
    {
        public static void CaptureException(Action toInvoke)
        {
            CaptureExceptionInternal(
                () =>
                {
                    toInvoke();
                    return Task.CompletedTask;
                },
                isAsync: false
            ).GetAwaiter().GetResult();
        }

        public static Task CaptureException(Func<Task> toInvoke)
        {
            return CaptureExceptionInternal(toInvoke, isAsync: true);
        }

        private static async Task CaptureExceptionInternal(Func<Task> toInvoke, bool isAsync)
        {
            try
            {
                await toInvoke();
            }
            catch (Exception ex)
            {
                await HandleException(ex);
            }
        }

        private static async Task HandleException(Exception ex)
        {
            var currentPage = GetCurrentPage();

            if (currentPage != null)
            {
                await currentPage.DisplayAlert(
                    "Error",
                    $"An error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "OK"
                );
            }
            else
            {
                Debug.WriteLine($"Error: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        private static Page GetCurrentPage()
        {
            return Application.Current?.MainPage is NavigationPage navigationPage
                ? navigationPage.CurrentPage
                : Application.Current?.MainPage;
        }
    }
}