using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Models;
using Deaddit.Extensions;
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
                            isDragging = true;
                            startX = e.touches[0].clientX;
                            startY = e.touches[0].clientY;
                            startPosX = posX;
                            startPosY = posY;
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
                            const dx = e.touches[0].clientX - startX;
                            const dy = e.touches[0].clientY - startY;
                            posX = startPosX + dx;
                            posY = startPosY + dy;
                            clampPosition();
                            updateTransform();
                        }
                    }, { passive: false });

                    container.addEventListener('touchend', (e) => {
                        if (e.touches.length < 2) {
                            isPinching = false;
                        }
                        if (e.touches.length === 0) {
                            isDragging = false;
                            // Reset to scale 1 if barely zoomed
                            if (scale < 1.05) {
                                scale = 1;
                                posX = 0;
                                posY = 0;
                                updateTransform();
                            }
                        }
                    });

                    // Double tap to reset
                    let lastTap = 0;
                    container.addEventListener('touchend', (e) => {
                        if (e.touches.length === 0 && !isPinching) {
                            const now = Date.now();
                            if (now - lastTap < 300) {
                                scale = 1;
                                posX = 0;
                                posY = 0;
                                updateTransform();
                            }
                            lastTap = now;
                        }
                    });
                </script>
            </body>
            </html>
            """;

        private readonly ApplicationStyling _applicationStyling;

        private readonly FileDownload[] _postItems;

        public EmbeddedImage(ApplicationStyling applicationTheme, FileDownload[] items)
        {
            this.InitializeComponent();
            _applicationStyling = applicationTheme;
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
            await Share.Default.ShareFiles("", _postItems);
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