using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Models;
using Deaddit.Extensions;
using Deaddit.Utils;
using System.Diagnostics;

namespace Deaddit
{
    public partial class EmbeddedVideo : ContentPage
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly FileDownload _postItems;

        public EmbeddedVideo(FileDownload fileDownload, ApplicationStyling applicationTheme)
        {
            _applicationStyling = applicationTheme;
            _postItems = fileDownload;

            this.InitializeComponent();
            navigationBar.BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor();
            mediaView.BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
            saveButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            shareButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            mediaView.Source = new Uri(fileDownload.LaunchUrl);
        }

        public async void OnShareClicked(object? sender, EventArgs e)
        {
            await Share.Default.ShareFiles("", _postItems);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                mediaView.Stop();
                mediaView.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
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
    }
}