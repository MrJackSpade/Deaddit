using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Extensions;
using Deaddit.Utils;
using System.Diagnostics;

namespace Deaddit
{
    public partial class EmbeddedVideo : ContentPage
    {
        private readonly ApplicationStyling _applicationStyling;

        private readonly IStreamConverter? _converter;

        private readonly IDisplayMessages _displayMessages;

        private readonly FileDownload _postItems;

        private CancellationTokenSource? _downloadCts;

        public EmbeddedVideo(FileDownload fileDownload, ApplicationStyling applicationTheme, IDisplayMessages displayMessages, IStreamConverter? converter)
        {
            _applicationStyling = applicationTheme;
            _displayMessages = displayMessages;
            _converter = converter;
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
            _downloadCts?.Cancel();
            _downloadCts = new CancellationTokenSource();

            downloadOverlay.IsVisible = true;
            downloadIndicator.IsRunning = true;

            try
            {
                await Share.Default.ShareFiles("", _postItems, _converter);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await _displayMessages.DisplayException(ex);
            }
            finally
            {
                downloadIndicator.IsRunning = false;
                downloadOverlay.IsVisible = false;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _downloadCts?.Cancel();

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
            _downloadCts?.Cancel();
            _downloadCts = new CancellationTokenSource();

            downloadOverlay.IsVisible = true;
            downloadIndicator.IsRunning = true;

            try
            {
                await FileStorage.Save(_postItems, _converter, _downloadCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await _displayMessages.DisplayException(ex);
            }
            finally
            {
                downloadIndicator.IsRunning = false;
                downloadOverlay.IsVisible = false;
            }
        }
    }
}