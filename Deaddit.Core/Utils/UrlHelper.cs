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

        public static bool IsHiddenHost(string? host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            return _hiddenHosts.Contains(host);
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