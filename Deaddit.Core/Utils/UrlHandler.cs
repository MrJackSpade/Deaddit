using Deaddit.Core.Reddit.Models;
using MimeKit;

namespace Deaddit.Core.Utils
{
    public static class UrlHandler
    {
        public static string GetMimeTypeFromUri(Uri uri)
        {
            // Strip the parameters from the URI if they exist
            Uri uriWithoutParams = new(uri.GetLeftPart(UriPartial.Path));

            // Get the file extension from the URI
            string fileExtension = Path.GetExtension(uriWithoutParams.AbsolutePath);

            if (string.IsNullOrEmpty(fileExtension))
            {
                return "application/octet-stream"; // Default to a generic binary MIME type
            }

            // Get the MIME type based on the file extension
            string mimeType = MimeTypes.GetMimeType(fileExtension);

            return mimeType ?? "application/octet-stream"; // Return a generic MIME type if not found
        }

        public static PostTarget Resolve(string url)
        {
            string mimeType = GetMimeTypeFromUri(new Uri(url));

            // Switch based on the type
            if (mimeType.StartsWith("image/"))
            {
                return new PostTarget(PostTargetKind.Image, url);
            }
            else if (mimeType.StartsWith("audio/"))
            {
                return new PostTarget(PostTargetKind.Audio, url);
            }
            else if (mimeType.StartsWith("video/"))
            {
                return new PostTarget(PostTargetKind.Video, url);
            }

            return new PostTarget(PostTargetKind.Url, url);
        }
    }
}