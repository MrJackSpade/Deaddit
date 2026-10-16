using Deaddit.Core.Interfaces;

namespace Deaddit.Utils
{
    /// <summary>
    /// Opens a URL clicked inside a page's web content. Clicks arrive on an async void event
    /// handler, where an escaping exception terminates the app, so a failed launch is reported
    /// to the user here instead.
    /// </summary>
    internal static class UrlClickLauncher
    {
        public static async Task Launch(Page page, string url, IAggregateUrlHandler urlHandler, IAggregatePostHandler postHandler, IDisplayMessages displayMessages)
        {
            try
            {
                if (!urlHandler.CanLaunch(url, postHandler))
                {
                    await page.DisplayAlert("Alert", $"Can not handle url {url}", "OK");
                    return;
                }

                await urlHandler.Launch(url, postHandler);
            }
            catch (Exception ex)
            {
                await displayMessages.DisplayException(ex);
            }
        }
    }
}
