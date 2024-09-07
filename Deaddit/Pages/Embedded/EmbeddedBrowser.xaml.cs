using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models;
using Deaddit.Extensions;

namespace Deaddit
{
    public partial class EmbeddedBrowser : ContentPage
    {
        private readonly ApplicationStyling _applicationTheme;

        public EmbeddedBrowser(PostItems items, ApplicationStyling applicationTheme)
        {
            this.InitializeComponent();

            PostItem item = items.Items.Single();

            string url = item.LaunchUrl;

            //Embedding a browser in a MAUI app will not allow http requests, so we need to change the url to https
            if (url.StartsWith("http://"))
            {
                url = url.Replace("http://", "https://");
            }

            _applicationTheme = applicationTheme;
            navigationBar.BackgroundColor = _applicationTheme.PrimaryColor.ToMauiColor();
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