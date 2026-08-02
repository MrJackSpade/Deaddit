using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Utils;
using Deaddit.Extensions;
using Deaddit.Utils;
using System.Net;
using System.Text;

namespace Deaddit
{
    public partial class EmbeddedBrowser : ContentPage
    {
        private const string READER_TEMPLATE = """
            <!DOCTYPE html>
            <html>
            <head>
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <style>
                    body {
                        background-color: %BACKGROUND_COLOR%;
                        color: %TEXT_COLOR%;
                        font-family: -apple-system, Roboto, sans-serif;
                        font-size: %BODY_FONT_SIZE%px;
                        line-height: 1.6;
                        margin: 0;
                        padding: 16px;
                        overflow-wrap: break-word;
                    }
                    h1 { font-size: %TITLE_FONT_SIZE%px; line-height: 1.3; margin: 0 0 8px 0; }
                    h2, h3, h4, h5, h6 { line-height: 1.3; margin: 1.4em 0 0.4em 0; }
                    .byline {
                        color: %SUBTEXT_COLOR%;
                        font-size: %SUBTEXT_FONT_SIZE%px;
                        margin: 0 0 20px 0;
                        border-bottom: 1px solid %SUBTEXT_COLOR%;
                        padding-bottom: 12px;
                    }
                    a { color: %LINK_COLOR%; }
                    img, video, figure { max-width: 100%; height: auto; margin: 0 auto; display: block; }
                    figcaption { color: %SUBTEXT_COLOR%; font-size: %SUBTEXT_FONT_SIZE%px; text-align: center; }
                    pre {
                        background-color: %CODE_COLOR%;
                        padding: 12px;
                        overflow-x: auto;
                        border-radius: 4px;
                    }
                    code { background-color: %CODE_COLOR%; border-radius: 3px; }
                    blockquote {
                        border-left: 3px solid %SUBTEXT_COLOR%;
                        margin-left: 0;
                        padding-left: 12px;
                        color: %SUBTEXT_COLOR%;
                    }
                    table { max-width: 100%; display: block; overflow-x: auto; }
                </style>
            </head>
            <body>
                <h1>%TITLE%</h1>
                <div class="byline">%BYLINE%</div>
                %CONTENT%
            </body>
            </html>
            """;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IDisplayMessages _displayMessages;

        private readonly string _url;

        private string _currentUrl;

        private bool _readerMode;

        public EmbeddedBrowser(string url, ApplicationStyling applicationTheme, IDisplayMessages displayMessages)
        {
            _url = url;
            _displayMessages = displayMessages;

            this.InitializeComponent();

            //Embedding a browser in a MAUI app will not allow http requests, so we need to change the url to https
            if (url.StartsWith("http://"))
            {
                url = url.Replace("http://", "https://");
            }

            _currentUrl = url;

            _applicationStyling = applicationTheme;
            saveButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            shareButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            readerButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            navigationBar.BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor();

            webView.Source = new Uri(url);
        }

        public void OnBackClicked(object? sender, EventArgs e)
        {
            // Logic to go back, for example:
            Navigation.PopAsync();
        }

        public async void OnBrowserClicked(object? sender, EventArgs e)
        {
            try
            {
                Uri uri = new(_url);
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception)
            {
                // An unexpected error occurred. No browser may be installed on the device.
            }
        }

        public void OnBypassClicked(object? sender, EventArgs e)
        {
            webView.Source = new UrlWebViewSource
            {
                Url = "https://www.removepaywall.com/search?url=" + _url
            };
        }

        public async void OnReaderClicked(object? sender, EventArgs e)
        {
            if (_readerMode)
            {
                this.ExitReaderMode();
                return;
            }

            try
            {
                await BusyOverlay.RunAsync(this, this.EnterReaderMode);
            }
            catch (Exception ex)
            {
                await _displayMessages.DisplayException(ex);
            }
        }

        public void OnSaveClicked(object? sender, EventArgs e)
        {
            // Logic to save current state or media
        }

        public void OnShareClicked(object? sender, EventArgs e)
        {
            Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = _url
            });
        }

        /// <summary>
        /// Tracks the live page as the user navigates, and drops out of reader mode when they
        /// follow a link out of the extracted article.
        /// </summary>
        public void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _currentUrl = e.Url;

            if (_readerMode)
            {
                _readerMode = false;
                readerButton.TextColor = _applicationStyling.TextColor.ToMauiColor();
            }
        }

        private async Task EnterReaderMode()
        {
            string? html = await this.ExtractPageHtml();

            if (string.IsNullOrWhiteSpace(html))
            {
                await _displayMessages.DisplayMessage("Couldn't read the page contents.");
                return;
            }

            //Readability over a full page is CPU heavy enough to stall the UI thread
            ReaderArticle? article = await Task.Run(() => ReaderModeExtractor.Extract(_currentUrl, html));

            if (article is null)
            {
                await _displayMessages.DisplayMessage("No article found on this page.");
                return;
            }

            _readerMode = true;
            readerButton.TextColor = _applicationStyling.HyperlinkColor.ToMauiColor();

            webView.Source = new HtmlWebViewSource
            {
                Html = this.RenderArticle(article)
            };
        }

        private void ExitReaderMode()
        {
            _readerMode = false;
            readerButton.TextColor = _applicationStyling.TextColor.ToMauiColor();
            webView.Source = new UrlWebViewSource { Url = _currentUrl };
        }

        /// <summary>
        /// Pulls the live, post-script DOM back out of the WebView. The payload is base64'd inside
        /// the page because raw HTML does not survive the round trip through EvaluateJavaScriptAsync.
        /// </summary>
        private async Task<string?> ExtractPageHtml()
        {
            string? result = await webView.EvaluateJavaScriptAsync(
                "btoa(unescape(encodeURIComponent(document.documentElement.outerHTML)))");

            if (string.IsNullOrWhiteSpace(result) || result == "null")
            {
                return null;
            }

            //Android hands the result back as a quoted JS string literal
            result = result.Trim('"');

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(result));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private string RenderArticle(ReaderArticle article)
        {
            List<string> byline = [];

            if (!string.IsNullOrWhiteSpace(article.Author))
            {
                byline.Add(article.Author);
            }

            if (!string.IsNullOrWhiteSpace(article.SiteName))
            {
                byline.Add(article.SiteName);
            }

            if (article.PublicationDate.HasValue)
            {
                byline.Add(article.PublicationDate.Value.ToString("MMMM d, yyyy"));
            }

            if (article.TimeToRead > TimeSpan.Zero)
            {
                byline.Add($"{Math.Max(1, (int)article.TimeToRead.TotalMinutes)} min read");
            }

            return READER_TEMPLATE
                .Replace("%BACKGROUND_COLOR%", _applicationStyling.PrimaryColor.ToHex())
                .Replace("%TEXT_COLOR%", _applicationStyling.TextColor.ToHex())
                .Replace("%SUBTEXT_COLOR%", _applicationStyling.SubTextColor.ToHex())
                .Replace("%LINK_COLOR%", _applicationStyling.HyperlinkColor.ToHex())
                .Replace("%CODE_COLOR%", _applicationStyling.SecondaryColor.ToHex())
                .Replace("%BODY_FONT_SIZE%", _applicationStyling.TitleFontSize.ToString())
                .Replace("%TITLE_FONT_SIZE%", (_applicationStyling.TitleFontSize * 1.6).ToString())
                .Replace("%SUBTEXT_FONT_SIZE%", _applicationStyling.SubTextFontSize.ToString())
                .Replace("%TITLE%", WebUtility.HtmlEncode(article.Title ?? string.Empty))
                .Replace("%BYLINE%", WebUtility.HtmlEncode(string.Join(" · ", byline)))
                .Replace("%CONTENT%", article.Content);
        }
    }
}
