using Deaddit.Core.Configurations.Models;
using Deaddit.Extensions;

namespace Deaddit
{
    public partial class EmbeddedBrowser : ContentPage
    {
        private readonly ApplicationStyling _applicationStyling;

        public EmbeddedBrowser(string url, ApplicationStyling applicationTheme)
        {
            this.InitializeComponent();

            //Embedding a browser in a MAUI app will not allow http requests, so we need to change the url to https
            if (url.StartsWith("http://"))
            {
                url = url.Replace("http://", "https://");
            }

            _applicationStyling = applicationTheme;
            saveButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            shareButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            navigationBar.BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor();

            webView.Source = new Uri(url);
        }

        public void OnBackClicked(object? sender, EventArgs e)
        {
            // Logic to go back, for example:
            Navigation.PopAsync();
        }

        public void OnSaveClicked(object? sender, EventArgs e)
        {
            // Logic to save current state or media
        }

        public void OnShareClicked(object? sender, EventArgs e)
        {
            Share.Default.RequestAsync(new ShareTextRequest
            {
                Uri = webView.Source.ToString()
            });
        }
    }
}