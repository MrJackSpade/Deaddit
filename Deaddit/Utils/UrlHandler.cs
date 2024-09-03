using Deaddit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Utils
{
    public static class UrlHandler
    {
        public static string GetMimeTypeFromUri(Uri uri)
        {
            // Strip the parameters from the URI if they exist
            var uriWithoutParams = new Uri(uri.GetLeftPart(UriPartial.Path));

            // Get the file extension from the URI
            string fileExtension = Path.GetExtension(uriWithoutParams.AbsolutePath);

            if (string.IsNullOrEmpty(fileExtension))
            {
                return "application/octet-stream"; // Default to a generic binary MIME type
            }

            // Get the MIME type based on the file extension
            string mimeType = MimeKit.MimeTypes.GetMimeType(fileExtension);

            return mimeType ?? "application/octet-stream"; // Return a generic MIME type if not found
        }

        public static RedditResource Resolve(string url)
        {
            string mimeType = GetMimeTypeFromUri(new Uri(url));

            // Switch based on the type
            if (mimeType.StartsWith("image/"))
            {
                return new RedditResource(RedditResourceKind.Image, url);
            }
            else if (mimeType.StartsWith("audio/"))
            {
                return new RedditResource(RedditResourceKind.Audio, url);
            }
            else if (mimeType.StartsWith("video/"))
            {
                return new RedditResource(RedditResourceKind.Video, url);
            }

            return new RedditResource(RedditResourceKind.Url, url);
        }
    }
}