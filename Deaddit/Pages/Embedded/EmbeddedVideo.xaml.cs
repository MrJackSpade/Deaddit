using Deaddit.Core.Configurations.Models;
using Deaddit.Extensions;

namespace Deaddit
{
    public partial class EmbeddedVideo : ContentPage
    {
        private readonly ApplicationStyling _applicationTheme;

        public EmbeddedVideo(string url, ApplicationStyling applicationTheme)
        {
            _applicationTheme = applicationTheme;
            this.InitializeComponent();
            navigationBar.BackgroundColor = _applicationTheme.PrimaryColor.ToMauiColor();
            mediaView.BackgroundColor = _applicationTheme.SecondaryColor.ToMauiColor();
            mediaView.Source = new Uri(url);
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