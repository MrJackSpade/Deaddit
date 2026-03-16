using Loxifi.FFmpeg.Transcoding;
using System.Xml.Linq;

namespace Deaddit.Core.Utils.IO
{
    public static class FileStreamService
    {
        public static async Task<Stream> GetStream(string url)
        {
            string fName = Path.GetFileName(url);

            if (fName.Contains('?'))
            {
                fName = fName[..fName.IndexOf('?')];
            }

            string ext = Path.GetExtension(fName);

            if (string.Equals(ext.Trim('.'), "m3u8", StringComparison.OrdinalIgnoreCase))
            {
                M3U8Downloader.MediaData mediaData = await M3U8Downloader.DownloadM3U8Async(url);
                return new MemoryStream(mediaData.VideoData);
            }
            else if (string.Equals(ext.Trim('.'), "mpd", StringComparison.OrdinalIgnoreCase))
            {
                return await GetMuxedDashStream(url);
            }
            else
            {
                return await new HttpClient().GetStreamAsync(url);
            }
        }

        private static async Task<Stream> GetMuxedDashStream(string dashUrl)
        {
            using HttpClient client = new();

            string manifest = await client.GetStringAsync(dashUrl);
            XDocument doc = XDocument.Parse(manifest);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            string baseUrl = dashUrl[..(dashUrl.LastIndexOf('/') + 1)];

            // Find video URL (highest bandwidth)
            string? videoBaseUrl = doc.Descendants(ns + "AdaptationSet")
                .Where(e => e.Attribute("contentType")?.Value == "video")
                .SelectMany(e => e.Descendants(ns + "Representation"))
                .OrderByDescending(e => int.TryParse(e.Attribute("bandwidth")?.Value, out int bw) ? bw : 0)
                .Select(e => e.Descendants(ns + "BaseURL").FirstOrDefault()?.Value)
                .FirstOrDefault();

            // Find audio URL
            string? audioBaseUrl = doc.Descendants(ns + "AdaptationSet")
                .Where(e => e.Attribute("contentType")?.Value == "audio")
                .SelectMany(e => e.Descendants(ns + "Representation"))
                .Select(e => e.Descendants(ns + "BaseURL").FirstOrDefault()?.Value)
                .FirstOrDefault();

            if (videoBaseUrl == null)
            {
                throw new InvalidOperationException("No video stream found in DASH manifest");
            }

            byte[] videoData = await client.GetByteArrayAsync(baseUrl + videoBaseUrl);

            if (audioBaseUrl != null)
            {
                try
                {
                    byte[] audioData = await client.GetByteArrayAsync(baseUrl + audioBaseUrl);

                    MemoryStream videoStream = new(videoData);
                    MemoryStream audioStream = new(audioData);
                    MemoryStream output = new();
                    MediaOperations.Mux(videoStream, audioStream, output);
                    output.Position = 0;
                    return output;
                }
                catch
                {
                    // Audio fetch/mux failed, return video only
                }
            }

            return new MemoryStream(videoData);
        }
    }
}