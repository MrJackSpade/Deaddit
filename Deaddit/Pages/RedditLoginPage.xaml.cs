using Deaddit.Core.Reddit;

namespace Deaddit.Pages
{
    public partial class RedditLoginPage : ContentPage
    {
        private readonly TaskCompletionSource<WebLogin?> _loginSource = new();

        private bool _resolved;

        private bool _checking;

        public Task<WebLogin?> LoginTask => _loginSource.Task;

        public RedditLoginPage()
        {
            this.InitializeComponent();
            loginWebView.Source = "https://www.reddit.com/login";

            // Poll for the token cookie since login happens via XHR
            // and doesn't trigger a Navigated event
            Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (_resolved)
                {
                    return false;
                }

                _ = this.CheckForLogin();
                return !_resolved;
            });
        }

        private async Task CheckForLogin()
        {
            if (_checking || _resolved)
            {
                return;
            }

            _checking = true;

            WebLogin? login;

            try
            {
                login = await this.TryGetLogin();
            }
            catch (Exception ex)
            {
                // Surface the failure to whoever awaits the login instead of polling forever.
                _resolved = true;
                _loginSource.TrySetException(ex);
                await this.CloseAsync();
                return;
            }

            if (login != null)
            {
                _resolved = true;
                _loginSource.TrySetResult(login);
                await this.CloseAsync();
            }

            _checking = false;
        }

        private async Task CloseAsync()
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }

        /// <summary>
        /// Returns the login once the browser holds a logged-in, unexpired token_v2, together with
        /// the complete reddit.com cookie header of that browser session. A token left over from an
        /// earlier session may be expired; Reddit replaces it as the page loads, so keep polling.
        /// </summary>
        private async Task<WebLogin?> TryGetLogin()
        {
            Dictionary<string, string> cookies = await this.GetRedditCookies();

            if (!cookies.TryGetValue("token_v2", out string? accessToken))
            {
                return null;
            }

            TokenV2 token = TokenV2.Parse(accessToken);

            if (!token.IsLoggedIn || token.IsExpired)
            {
                return null;
            }

            string cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}"));
            return new WebLogin(accessToken, token.Expiration, cookieHeader);
        }

        private async Task<Dictionary<string, string>> GetRedditCookies()
        {
            Dictionary<string, string> result = [];
#if ANDROID
            string? cookies = Android.Webkit.CookieManager.Instance?.GetCookie("https://www.reddit.com");

            if (cookies != null)
            {
                foreach (string cookie in cookies.Split(';'))
                {
                    string[] pair = cookie.Trim().Split('=', 2);

                    if (pair.Length == 2)
                    {
                        result[pair[0]] = pair[1];
                    }
                }
            }
#elif WINDOWS
            Microsoft.UI.Xaml.Controls.WebView2? platformView = loginWebView.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.WebView2;

            if (platformView?.CoreWebView2 != null)
            {
                foreach (Microsoft.Web.WebView2.Core.CoreWebView2Cookie cookie in await platformView.CoreWebView2.CookieManager.GetCookiesAsync("https://www.reddit.com"))
                {
                    result[cookie.Name] = cookie.Value;
                }
            }
#endif
            await Task.CompletedTask;
            return result;
        }
    }
}
