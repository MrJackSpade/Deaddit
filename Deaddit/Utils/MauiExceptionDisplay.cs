using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Exceptions;
using System.Diagnostics;

namespace Deaddit.Utils
{
    internal class MauiExceptionDisplay : IDisplayExceptions
    {
        public async Task<bool> DisplayException(Exception exception)
        {
            try
            {
                return await this.HandleException(exception);
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

        private async Task<bool> HandleException(Exception ex)
        {
            Page currentPage = this.GetCurrentPage();

            if (currentPage != null)
            {
                if (ex is DisplayException de)
                {
                    await currentPage.DisplayAlert(
                        "Alert",
                        de.Message,
                        "OK"
                    );
                }
                else
                {
                    await currentPage.DisplayAlert(
                        "Error",
                        $"An error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                        "OK"
                    );
                }

                return true;
            }
            else
            {
                Debug.WriteLine($"Error: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }

            return false;
        }
    }
}