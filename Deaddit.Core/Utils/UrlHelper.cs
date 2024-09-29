using Deaddit.Core.Reddit.Models;
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

        public static PostItems Resolve(string url)
        {
            string testUrl = url;

            if (testUrl.Contains('?'))
            {
                testUrl = testUrl[..testUrl.IndexOf('?')];
            }

            string mimeType = GetMimeTypeFromUri(new Uri(testUrl));

            PostItems items;
            // Switch based on the type
            if (mimeType.StartsWith("image/"))
            {
                items = new(PostTargetKind.Image);
            }
            else if (mimeType.StartsWith("audio/"))
            {
                items = new(PostTargetKind.Audio);
            }
            else if (mimeType.StartsWith("video/"))
            {
                items = new(PostTargetKind.Video);
            }
            else
            {
                items = new(PostTargetKind.Url);
            }

            items.Add(new PostItem()
            {
                DownloadUrl = url,
                LaunchUrl = url,
                FileName = UrlHelper.GetFileName(url)
            });

            return items;
        }
    }
}