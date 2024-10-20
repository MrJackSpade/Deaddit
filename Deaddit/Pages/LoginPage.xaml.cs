using Deaddit.Core.Configurations.Models;
using Maui.WebComponents;

namespace Deaddit.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        public async Task<RedditCredentials> Login()
        {
            webElement.Source = new UrlWebViewSource() { Url = "https://www.reddit.com/login" };

            await this.WaitFor("blah blah blah");

            return new RedditCredentials();
        }

        public async Task WaitFor(string selector, CancellationToken cancellationToken = default)
        {
            var interval = TimeSpan.FromMilliseconds(500);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Execute JavaScript to check if the element exists
                    var js = $"document.querySelector('{selector}') !== null";
                    var result = await webElement.EvaluateJavaScriptAsync(js);

                    if (bool.TryParse(result.ToString(), out bool exists) && exists)
                    {
                        return; // Element found
                    }
                }
                catch (Exception)
                {
                    // Handle exceptions if needed
                }

                await Task.Delay(interval, cancellationToken);
            }

            // Operation was cancelled
            throw new OperationCanceledException("WaitFor operation was cancelled.");
        }
    }
}