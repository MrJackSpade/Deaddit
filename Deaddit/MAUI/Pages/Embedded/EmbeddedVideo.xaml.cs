using Deaddit.Configurations.Models;

namespace Deaddit
{
    public partial class EmbeddedVideo : ContentPage
    {
        private readonly ApplicationTheme _applicationTheme;

        public EmbeddedVideo(string url, ApplicationTheme applicationTheme)
        {
            _applicationTheme = applicationTheme;
            this.InitializeComponent();
            navigationBar.BackgroundColor = _applicationTheme.PrimaryColor;
            mediaView.BackgroundColor = _applicationTheme.SecondaryColor;
            mediaView.Source = new Uri(url);
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