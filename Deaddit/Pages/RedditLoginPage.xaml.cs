namespace Deaddit.Pages
{
    public partial class RedditLoginPage : ContentPage
    {
        private readonly TaskCompletionSource<string?> _tokenSource = new();

        private bool _resolved;

        private bool _checking;

        public Task<string?> TokenTask => _tokenSource.Task;

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

                _ = this.CheckForToken();
                return !_resolved;
            });
        }

        private async Task CheckForToken()
        {
            if (_checking || _resolved)
            {
                return;
            }

            _checking = true;

            string? token = await this.TryGetTokenCookie();

            if (token != null)
            {
                _resolved = true;
                _tokenSource.TrySetResult(token);

                if (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopAsync();
                }
            }

            _checking = false;
        }

        private async Task<string?> TryGetTokenCookie()
        {
#if ANDROID
            string? cookies = Android.Webkit.CookieManager.Instance?.GetCookie("https://www.reddit.com");

            if (cookies != null)
            {
                foreach (string cookie in cookies.Split(';'))
                {
                    string trimmed = cookie.Trim();

                    if (trimmed.StartsWith("token_v2="))
                    {
                        string token = trimmed["token_v2=".Length..];

                        if (this.IsLoggedInToken(token))
                        {
                            return token;
                        }
                    }
                }
            }
#elif WINDOWS
            Microsoft.UI.Xaml.Controls.WebView2? platformView = loginWebView.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.WebView2;

            if (platformView?.CoreWebView2 != null)
            {
                List<Microsoft.Web.WebView2.Core.CoreWebView2Cookie> cookies =
                    (await platformView.CoreWebView2.CookieManager.GetCookiesAsync("https://www.reddit.com")).ToList();

                Microsoft.Web.WebView2.Core.CoreWebView2Cookie? tokenCookie =
                    cookies.FirstOrDefault(c => c.Name == "token_v2");

                if (tokenCookie != null && this.IsLoggedInToken(tokenCookie.Value))
                {
                    return tokenCookie.Value;
                }
            }
#endif
            await Task.CompletedTask;
            return null;
        }

        private bool IsLoggedInToken(string jwt)
        {
            try
            {
                string[] parts = jwt.Split('.');

                if (parts.Length != 3)
                {
                    return false;
                }

                string payload = parts[1].Replace('-', '+').Replace('_', '/');

                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                byte[] bytes = Convert.FromBase64String(payload);
                string json = System.Text.Encoding.UTF8.GetString(bytes);

                return !json.Contains("\"sub\":\"loid\"");
            }
            catch
            {
                return false;
            }
        }
    }
}
