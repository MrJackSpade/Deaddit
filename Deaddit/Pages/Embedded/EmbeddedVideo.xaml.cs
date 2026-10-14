using CommunityToolkit.Maui.Core;
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
        private static readonly HttpClient ProbeClient = new();

        private readonly ApplicationStyling _applicationStyling;

        private readonly IStreamConverter? _converter;

        private readonly IDisplayMessages _displayMessages;

        private readonly FileDownload _postItems;

        private readonly SavePathConfiguration _savePaths;

        private CancellationTokenSource? _downloadCts;

        public EmbeddedVideo(FileDownload fileDownload, ApplicationStyling applicationTheme, SavePathConfiguration savePaths, IDisplayMessages displayMessages, IStreamConverter? converter)
        {
            _applicationStyling = applicationTheme;
            _savePaths = savePaths;
            _displayMessages = displayMessages;
            _converter = converter;
            _postItems = fileDownload;

            this.InitializeComponent();
            navigationBar.BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor();
            mediaView.BackgroundColor = _applicationStyling.SecondaryColor.ToMauiColor();
            saveButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            shareButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            mediaView.MediaFailed += this.MediaView_MediaFailed;
            mediaView.Source = new Uri(fileDownload.LaunchUrl);
        }

        private async void MediaView_MediaFailed(object? sender, MediaFailedEventArgs e)
        {
            // Without this a video the player can't load is just a black screen. Ask the server
            // why: Reddit answers 403 for videos it has pulled while the post stays up (reddit.com
            // shows "This video is no longer available" for those), which the player only reports
            // as a generic source error.
            string message;

            try
            {
                using HttpRequestMessage probe = new(HttpMethod.Get, _postItems.LaunchUrl);
                using HttpResponseMessage response = await ProbeClient.SendAsync(probe, HttpCompletionOption.ResponseHeadersRead);

                message = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "This video is no longer available. Reddit has removed the video file (the post itself is still up)."
                    : $"The video failed to play: {e.ErrorMessage}\n\nServer response: {(int)response.StatusCode} {response.StatusCode}";
            }
            catch (HttpRequestException ex)
            {
                // This handler is async void: an escaping exception would crash the app. Report both failures instead.
                message = $"The video failed to play: {e.ErrorMessage}\n\nThe server could not be reached either: {ex.Message}";
            }

            await MainThread.InvokeOnMainThreadAsync(() => _displayMessages.DisplayMessage(message));
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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            DeviceDisplay.Current.KeepScreenOn = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            DeviceDisplay.Current.KeepScreenOn = false;

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
                await FileStorage.Save(_postItems, _converter, _savePaths, _downloadCts.Token);
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