using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Exceptions;
using System.Diagnostics;

namespace Deaddit.Utils
{
    internal class MauiExceptionDisplay : IDisplayMessages
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

        public async Task<bool> DisplayMessage(string message)
        {
            try
            {
                Page page = this.GetCurrentPage();

                if (page is null)
                {
                    return false;
                }

                await page.DisplayAlert(
                    "Alert",
                    message,
                    "OK"
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error displaying message: {ex.Message}\nStack Trace: {ex.StackTrace}");
                return false;
            }

            return true;
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