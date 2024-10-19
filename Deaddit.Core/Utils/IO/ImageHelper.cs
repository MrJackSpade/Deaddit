using SkiaSharp;

namespace Deaddit.Core.Utils.IO
{
    public static class ImageHelper
    {
        public static async Task<Stream> ResizeAndCropImageFromUrlAsync(string imageUrl, int size)
        {
            // Use HttpClient to download the image from the URL
            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Unable to download image.");
            }

            using Stream inputStream = await response.Content.ReadAsStreamAsync();
            // Load the image into a SkiaSharp bitmap
            using SKBitmap originalBitmap = SKBitmap.Decode(inputStream) ?? throw new Exception("Unable to decode image.");

            // Calculate aspect ratio of the original image
            float widthRatio = (float)size / originalBitmap.Width;
            float heightRatio = (float)size / originalBitmap.Height;
            float scaleRatio = Math.Max(widthRatio, heightRatio); // Ensures the image fills the size

            // Calculate the new width and height to maintain the aspect ratio
            int scaledWidth = (int)(originalBitmap.Width * scaleRatio);
            int scaledHeight = (int)(originalBitmap.Height * scaleRatio);

            // Resize the bitmap to the scaled dimensions
            using SKBitmap resizedBitmap = originalBitmap.Resize(new SKImageInfo(scaledWidth, scaledHeight), SKFilterQuality.High)
                ?? throw new Exception("Unable to resize image.");

            // Calculate cropping coordinates (center crop)
            int cropX = (scaledWidth - size) / 2;
            int cropY = (scaledHeight - size) / 2;

            // Define the cropping rectangle
            SKRectI cropRect = new(cropX, cropY, cropX + size, cropY + size);

            // Create a new bitmap for the cropped image
            using SKBitmap croppedBitmap = new(size, size);
            using SKCanvas canvas = new(croppedBitmap);

            // Draw the resized image onto the cropped bitmap
            canvas.DrawBitmap(resizedBitmap, cropRect, new SKRectI(0, 0, size, size));

            // Create an SKImage from the cropped bitmap
            using SKImage image = SKImage.FromBitmap(croppedBitmap);

            // Encode the image to a memory stream in JPEG format
            SKData encodedData = image.Encode(SKEncodedImageFormat.Jpeg, 100);
            return encodedData.AsStream();
        }

        public static async Task<Stream> ResizeLargeImageWithContainFitAsync(string imageUrl, int maxWidth, int maxHeight)
        {
            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(imageUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Unable to download image.");
            }

            using Stream inputStream = await response.Content.ReadAsStreamAsync();
            using SKBitmap originalBitmap = SKBitmap.Decode(inputStream) ?? throw new Exception("Unable to decode image.");

            // Check if resizing is necessary
            if (originalBitmap.Width <= maxWidth && originalBitmap.Height <= maxHeight)
            {
                // No resizing needed, return the original image
                using SKImage imagei = SKImage.FromBitmap(originalBitmap);
                SKData encodedDatai = imagei.Encode(SKEncodedImageFormat.Jpeg, 100);
                return encodedDatai.AsStream();
            }

            // Calculate the scaling factor to fit within the max dimensions while maintaining aspect ratio
            float widthRatio = (float)maxWidth / originalBitmap.Width;
            float heightRatio = (float)maxHeight / originalBitmap.Height;
            float scaleRatio = Math.Min(widthRatio, heightRatio);

            int newWidth = (int)(originalBitmap.Width * scaleRatio);
            int newHeight = (int)(originalBitmap.Height * scaleRatio);

            // Resize the bitmap to the new dimensions
            using SKBitmap resizedBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High)
                ?? throw new Exception("Unable to resize image.");

            // Create an SKImage from the resized bitmap
            using SKImage image = SKImage.FromBitmap(resizedBitmap);

            // Encode the image to a memory stream in JPEG format
            SKData encodedData = image.Encode(SKEncodedImageFormat.Jpeg, 100);
            return encodedData.AsStream();
        }
    }
}