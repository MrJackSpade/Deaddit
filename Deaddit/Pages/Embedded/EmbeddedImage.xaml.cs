using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Utils;
using System.Text;

namespace Deaddit
{
    public partial class EmbeddedImage : ContentPage
    {
        private const string TEMPLATE = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
                <title>Image Viewer</title>
                <style>
                    * {
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                    }
                    html, body {
                        width: 100%;
                        height: 100%;
                        overflow: hidden;
                        background-color: %BACKGROUND_COLOR%;
                    }
                    .container {
                        width: 100%;
                        height: 100%;
                        overflow: auto;
                        -webkit-overflow-scrolling: touch;
                    }
                    .container.zoomed {
                        overflow: hidden;
                    }
                    .image-wrapper {
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        min-height: 100%;
                        transform-origin: 0 0;
                        will-change: transform;
                    }
                    img {
                        display: block;
                        max-width: 100vw;
                        height: auto;
                    }
                </style>
            </head>
            <body>
                <div class="container" id="container">
                    <div class="image-wrapper" id="wrapper">
                        %IMAGES%
                    </div>
                </div>
                <script>
                    const container = document.getElementById('container');
                    const wrapper = document.getElementById('wrapper');

                    let scale = 1;
                    let minScale = 1;
                    let maxScale = 5;
                    let posX = 0;
                    let posY = 0;

                    let startX = 0;
                    let startY = 0;
                    let startPosX = 0;
                    let startPosY = 0;
                    let startDist = 0;
                    let startScale = 1;
                    let isPinching = false;
                    let isDragging = false;
                    let velocityX = 0;
                    let velocityY = 0;
                    let lastMoveTime = 0;
                    let lastMoveX = 0;
                    let lastMoveY = 0;
                    let animationId = null;

                    function isZoomed() {
                        return scale > 1.05;
                    }

                    function updateZoomState() {
                        if (isZoomed()) {
                            container.classList.add('zoomed');
                        } else {
                            container.classList.remove('zoomed');
                        }
                    }

                    function updateTransform() {
                        wrapper.style.transform = `translate(${posX}px, ${posY}px) scale(${scale})`;
                        updateZoomState();
                    }

                    function clampPosition() {
                        const containerRect = container.getBoundingClientRect();
                        const scaledWidth = wrapper.scrollWidth * scale;
                        const scaledHeight = wrapper.scrollHeight * scale;

                        if (scaledWidth <= containerRect.width) {
                            posX = (containerRect.width - scaledWidth) / 2;
                        } else {
                            const minX = containerRect.width - scaledWidth;
                            const maxX = 0;
                            posX = Math.min(maxX, Math.max(minX, posX));
                        }

                        if (scaledHeight <= containerRect.height) {
                            posY = (containerRect.height - scaledHeight) / 2;
                        } else {
                            const minY = containerRect.height - scaledHeight;
                            const maxY = 0;
                            posY = Math.min(maxY, Math.max(minY, posY));
                        }
                    }

                    function getDistance(touches) {
                        const dx = touches[0].clientX - touches[1].clientX;
                        const dy = touches[0].clientY - touches[1].clientY;
                        return Math.sqrt(dx * dx + dy * dy);
                    }

                    function getCenter(touches) {
                        return {
                            x: (touches[0].clientX + touches[1].clientX) / 2,
                            y: (touches[0].clientY + touches[1].clientY) / 2
                        };
                    }

                    container.addEventListener('touchstart', (e) => {
                        if (e.touches.length === 2) {
                            e.preventDefault();
                            isPinching = true;
                            isDragging = false;
                            startDist = getDistance(e.touches);
                            startScale = scale;
                            const center = getCenter(e.touches);
                            startX = center.x;
                            startY = center.y;
                            startPosX = posX;
                            startPosY = posY;
                        } else if (e.touches.length === 1 && isZoomed()) {
                            if (animationId) { cancelAnimationFrame(animationId); animationId = null; }
                            isDragging = true;
                            startX = e.touches[0].clientX;
                            startY = e.touches[0].clientY;
                            startPosX = posX;
                            startPosY = posY;
                            lastMoveTime = Date.now();
                            lastMoveX = startX;
                            lastMoveY = startY;
                            velocityX = 0;
                            velocityY = 0;
                            e.preventDefault();
                        }
                    }, { passive: false });

                    container.addEventListener('touchmove', (e) => {
                        if (isPinching && e.touches.length === 2) {
                            e.preventDefault();
                            const dist = getDistance(e.touches);
                            const newScale = Math.min(maxScale, Math.max(minScale, startScale * (dist / startDist)));

                            const center = getCenter(e.touches);
                            const scaleChange = newScale / scale;

                            posX = center.x - (center.x - posX) * scaleChange;
                            posY = center.y - (center.y - posY) * scaleChange;

                            scale = newScale;
                            clampPosition();
                            updateTransform();
                        } else if (isDragging && e.touches.length === 1) {
                            e.preventDefault();
                            const now = Date.now();
                            const currentX = e.touches[0].clientX;
                            const currentY = e.touches[0].clientY;
                            const dt = now - lastMoveTime;
                            if (dt > 0) {
                                velocityX = (currentX - lastMoveX) / dt * 16;
                                velocityY = (currentY - lastMoveY) / dt * 16;
                            }
                            lastMoveTime = now;
                            lastMoveX = currentX;
                            lastMoveY = currentY;
                            const dx = currentX - startX;
                            const dy = currentY - startY;
                            posX = startPosX + dx;
                            posY = startPosY + dy;
                            clampPosition();
                            updateTransform();
                        }
                    }, { passive: false });

                    function startMomentum() {
                        const friction = 0.95;
                        function animate() {
                            velocityX *= friction;
                            velocityY *= friction;
                            if (Math.abs(velocityX) < 0.5 && Math.abs(velocityY) < 0.5) {
                                animationId = null;
                                return;
                            }
                            posX += velocityX;
                            posY += velocityY;
                            clampPosition();
                            updateTransform();
                            animationId = requestAnimationFrame(animate);
                        }
                        animationId = requestAnimationFrame(animate);
                    }

                    container.addEventListener('touchend', (e) => {
                        if (e.touches.length < 2) {
                            isPinching = false;
                        }
                        if (e.touches.length === 0) {
                            const wasDragging = isDragging;
                            isDragging = false;
                            // Reset to scale 1 if barely zoomed
                            if (scale < 1.05) {
                                scale = 1;
                                posX = 0;
                                posY = 0;
                                updateTransform();
                            } else if (wasDragging && (Math.abs(velocityX) > 1 || Math.abs(velocityY) > 1)) {
                                startMomentum();
                            }
                        }
                    });

                    // Double tap/click to toggle between fit-to-window and 100%
                    let lastTap = 0;
                    let lastTapX = 0;
                    let lastTapY = 0;

                    function getActualSizeScale() {
                        const img = wrapper.querySelector('img');
                        if (!img || !img.naturalWidth) return 1;
                        const displayedWidth = img.offsetWidth;
                        return img.naturalWidth / displayedWidth;
                    }

                    function toggleZoom(clientX, clientY) {
                        const actualSizeScale = getActualSizeScale();
                        const isFitToWindow = scale < 1.05;

                        if (isFitToWindow && actualSizeScale > 1.05) {
                            // Zoom to 100% centered on tap/click position
                            const containerRect = container.getBoundingClientRect();
                            const targetScale = Math.min(actualSizeScale, maxScale);

                            // Calculate position to center zoom on click point
                            posX = clientX - (clientX - posX) * (targetScale / scale);
                            posY = clientY - (clientY - posY) * (targetScale / scale);
                            scale = targetScale;

                            clampPosition();
                        } else {
                            // Reset to fit-to-window
                            scale = 1;
                            posX = 0;
                            posY = 0;
                        }
                        updateTransform();
                    }

                    container.addEventListener('touchend', (e) => {
                        if (e.touches.length === 0 && !isPinching) {
                            const now = Date.now();
                            const touch = e.changedTouches[0];
                            if (now - lastTap < 300) {
                                toggleZoom(lastTapX, lastTapY);
                            }
                            lastTap = now;
                            lastTapX = touch.clientX;
                            lastTapY = touch.clientY;
                        }
                    });

                    // Mouse double-click support
                    container.addEventListener('dblclick', (e) => {
                        e.preventDefault();
                        toggleZoom(e.clientX, e.clientY);
                    });
                </script>
            </body>
            </html>
            """;

        private readonly IAppNavigator _appNavigator;

        private readonly ApplicationStyling _applicationStyling;

        private readonly IStreamConverter? _converter;

        private readonly IDisplayMessages _displayMessages;

        private readonly FileDownload[] _postItems;

        private readonly SavePathConfiguration _savePaths;

        private CancellationTokenSource? _downloadCts;

        public EmbeddedImage(ApplicationStyling applicationTheme, IAppNavigator appNavigator, SavePathConfiguration savePaths, IDisplayMessages displayMessages, IStreamConverter? converter, FileDownload[] items)
        {
            this.InitializeComponent();
            _applicationStyling = applicationTheme;
            _appNavigator = appNavigator;
            _savePaths = savePaths;
            _displayMessages = displayMessages;
            _converter = converter;
            _postItems = items;

            navigationBar.BackgroundColor = _applicationStyling.PrimaryColor.ToMauiColor();
            saveButton.TextColor = applicationTheme.TextColor.ToMauiColor();
            shareButton.TextColor = applicationTheme.TextColor.ToMauiColor();

            StringBuilder images = new();

            foreach (FileDownload s in items)
            {
                images.Append($"<img src='{s.LaunchUrl}' alt='Image' />");
            }

            string Html = TEMPLATE.Replace("%IMAGES%", images.ToString())
                              .Replace("%BACKGROUND_COLOR%", applicationTheme.SecondaryColor.ToHex());

            webView.Source = new HtmlWebViewSource() { Html = Html };
        }

        public async void OnShareClicked(object? sender, EventArgs e)
        {
            List<FileDownload>? selected = await this.PromptForSelection();

            if (selected is null || selected.Count == 0)
            {
                return;
            }

            _downloadCts?.Cancel();
            _downloadCts = new CancellationTokenSource();

            downloadOverlay.IsVisible = true;
            downloadIndicator.IsRunning = true;

            try
            {
                await Share.Default.ShareFiles("", selected, _converter);
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

        private void OnBackClicked(object? sender, EventArgs e)
        {
            _downloadCts?.Cancel();
            Navigation.PopAsync();
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            List<FileDownload>? selected = await this.PromptForSelection();

            if (selected is null || selected.Count == 0)
            {
                return;
            }

            _downloadCts?.Cancel();
            _downloadCts = new CancellationTokenSource();

            downloadOverlay.IsVisible = true;
            downloadIndicator.IsRunning = true;

            try
            {
                await FileStorage.Save(selected, _converter, _savePaths, _downloadCts.Token);
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

        private async Task<List<FileDownload>?> PromptForSelection()
        {
            if (_postItems.Length <= 1)
            {
                return [.. _postItems];
            }

            return await _appNavigator.SelectImages([.. _postItems]);
        }
    }
}