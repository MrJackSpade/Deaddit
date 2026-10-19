using MimeKit;

namespace Deaddit.Core.Utils
{
    public static class UrlHelper
    {
        private static readonly HashSet<string> _hiddenHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "i.redd.it",
            "v.redd.it",
            "www.reddit.com"
        };

        private static readonly HashSet<string> _imageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".heic", ".heif", ".gif"
        };

        private static readonly HashSet<string> _videoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".webm", ".mov", ".mkv", ".m4v", ".avi", ".gifv"
        };

        public static bool IsImageFile(string fileName)
        {
            return _imageExtensions.Contains(GetExtension(fileName));
        }

        public static bool IsVideoFile(string fileName)
        {
            return _videoExtensions.Contains(GetExtension(fileName));
        }

        /// <summary>
        /// The host to show beside a link post, or null when there is none worth showing:
        /// not a web URL (relative reddit paths parse as host-less file URIs on Unix),
        /// or a host reddit itself serves.
        /// </summary>
        public static string? GetDisplayHost(string? url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || string.IsNullOrEmpty(uri.Host)
                || _hiddenHosts.Contains(uri.Host))
            {
                return null;
            }

            return uri.Host;
        }

        public static string GetExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            if (fileName.Contains('?'))
            {
                fileName = fileName[..fileName.LastIndexOf('?')];
            }

            return Path.GetExtension(fileName);
        }

        public static string GetFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            if (fileName.Contains('?'))
            {
                fileName = fileName[..fileName.LastIndexOf('?')];
            }

            return Path.GetFileName(fileName);
        }

        public static string GetMimeTypeFromUri(string s)
        {
            return GetMimeTypeFromUri(new Uri(s));
        }

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
    }
}