using Deaddit.Interfaces;

namespace Deaddit
{
    public partial class EmbeddedVideo : ContentPage
    {
        private readonly IAppTheme _appTheme;

        public EmbeddedVideo(string url, IAppTheme appTheme)
        {
            _appTheme = appTheme;
            this.InitializeComponent();
            this.navigationBar.BackgroundColor = _appTheme.PrimaryColor;
            this.mediaView.BackgroundColor = _appTheme.SecondaryColor;
            this.mediaView.Source = new Uri(url);
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