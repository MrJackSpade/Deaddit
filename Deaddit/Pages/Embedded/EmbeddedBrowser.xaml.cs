using Deaddit.Interfaces;

namespace Deaddit
{
    public partial class EmbeddedBrowser : ContentPage
    {
        private readonly IAppTheme _appTheme;

        public EmbeddedBrowser(string url, IAppTheme appTheme)
        {
            this.InitializeComponent();

            _appTheme = appTheme;
            navigationBar.BackgroundColor = _appTheme.PrimaryColor;
            this.webView.Source = new Uri(url);
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