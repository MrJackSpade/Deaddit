using Deaddit.Core.Interfaces;
using System.Diagnostics;

namespace Deaddit.Utils
{
    internal class MauiExceptionDisplay : IDisplayExceptions
    {
        public async Task<bool> DisplayException(Exception exception)
        {
            try
            {
                await this.HandleException(exception);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error displaying exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }

            return false;
        }

        private Page GetCurrentPage()
        {
            return Application.Current?.MainPage is NavigationPage navigationPage
                ? navigationPage.CurrentPage
                : Application.Current?.MainPage;
        }

        private async Task HandleException(Exception ex)
        {
            Page currentPage = this.GetCurrentPage();

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
    }
}