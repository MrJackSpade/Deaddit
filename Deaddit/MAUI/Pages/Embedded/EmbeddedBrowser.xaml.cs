using Deaddit.Configurations.Models;

namespace Deaddit
{
    public partial class EmbeddedBrowser : ContentPage
    {
        private readonly ApplicationTheme _applicationTheme;

        public EmbeddedBrowser(string url, ApplicationTheme applicationTheme)
        {
            this.InitializeComponent();

            //Embedding a browser in a MAUI app will not allow http requests, so we need to change the url to https
            if (url.StartsWith("http://"))
            {
                url = url.Replace("http://", "https://");
            }

            _applicationTheme = applicationTheme;
            navigationBar.BackgroundColor = _applicationTheme.PrimaryColor;
            webView.Source = new Uri(url);
        }

        private void OnBackClicked(object? sender, EventArgs e)
        {
            // Logic to go back, for example:
            Navigation.PopAsync();
        }

        private void OnSaveClicked(object? sender, EventArgs e)
        {
            // Logic to save current state or media
        }
    }
}