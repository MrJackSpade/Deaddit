using Deaddit.Configurations.Models;

namespace Deaddit
{
    public partial class EmbeddedImage : ContentPage
    {
        private const string TEMPLATE = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>%URL%</title>
                <style>
                    html, body {
                        height: 100vh;
                        width: 100vw;
                        margin: 0;
                        padding: 0;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        background-color: %BACKGROUND_COLOR%;
                    }
                    img.small {
                        max-width: 100%;
                        max-height: 100%;
                        object-fit: contain;
                    }
                </style>
            </head>
            <body>
                <img id="image" src="%URL%" class="small">
                <script>
                    const img = document.getElementById('image');

                    img.addEventListener('dblclick', () => {
                        img.classList.toggle('small');
                    });
                </script>
            </body>
            </html>
            """;

        private readonly ApplicationTheme _applicationTheme;

        public EmbeddedImage(string url, ApplicationTheme applicationTheme)
        {
            this.InitializeComponent();
            _applicationTheme = applicationTheme;
            navigationBar.BackgroundColor = _applicationTheme.PrimaryColor;

            string Html = TEMPLATE.Replace("%URL%", url)
                                  .Replace("%BACKGROUND_COLOR%", applicationTheme.SecondaryColor.ToHex());

            webView.Source = new HtmlWebViewSource() { Html = Html };
        }

        private void OnBackClicked(object sender, EventArgs e)
        {
            // Logic to go back, for example:
            Navigation.PopAsync();
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            // Logic to save current state or media
        }
    }
}