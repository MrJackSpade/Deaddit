using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models;
using Deaddit.Extensions;
using Deaddit.Utils;
using System.Text;

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
                        margin: 0;
                        padding: 0;
                        width: 100%;
                        background-color: %BACKGROUND_COLOR%;
                    }

                    body {
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        flex-direction: column;
                    }

                    body.small img {
                        max-width: 100%;
                        max-height: 100%;
                        object-fit: contain;
                        margin: auto;
                    }
                    body.large img {
                        width: auto;
                        height: auto;
                        max-width: none;
                        max-height: none;
                        margin: 0 auto;
                    }
                </style>
            </head>
            <body class="small" id="body">
                %IMAGES%
                <script>
                    const img = document.getElementById('body');
                    const body = document.getElementById('body');

                    img.addEventListener('dblclick', () => {
                        body.classList.toggle('small');
                        body.classList.toggle('large'); // Toggle between small and large for zooming
                    });
                </script>
            </body>
            </html>

            """;

        private readonly PostItems _postItems;

        private readonly ApplicationStyling _applicationTheme;

        public EmbeddedImage(ApplicationStyling applicationTheme, PostItems items)
        {
            this.InitializeComponent();
            _applicationTheme = applicationTheme;
            _postItems = items;

            navigationBar.BackgroundColor = _applicationTheme.PrimaryColor.ToMauiColor();
            saveButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            shareButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            StringBuilder images = new();

            foreach (PostItem s in items)
            {
                images.Append($"<img src='{s.LaunchUrl}' alt='Image' />");
            }

            string Html = TEMPLATE.Replace("%IMAGES%", images.ToString())
                              .Replace("%BACKGROUND_COLOR%", applicationTheme.SecondaryColor.ToHex());

            webView.Source = new HtmlWebViewSource() { Html = Html };
        }

        private void OnBackClicked(object? sender, EventArgs e)
        {
            // Logic to go back, for example:
            Navigation.PopAsync();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            await FileStorage.Save(_postItems);
        }

        public async void OnShareClicked(object? sender, EventArgs e)
        {
            await Share.Default.ShareFiles("", _postItems);
        }
    }
}