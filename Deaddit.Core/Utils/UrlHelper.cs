using MimeKit;

namespace Deaddit.Core.Utils
{
    public static class UrlHelper
    {
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