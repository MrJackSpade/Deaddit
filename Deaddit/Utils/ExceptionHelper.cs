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
            try
            {
                toInvoke();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public static async Task CaptureException(Func<Task> toInvoke)
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