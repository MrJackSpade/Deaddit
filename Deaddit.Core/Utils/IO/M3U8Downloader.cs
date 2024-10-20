namespace Deaddit.Core.Utils.IO
{
    public static class M3U8Downloader
    {
        public class MediaData
        {
            public byte[] AudioData { get; set; }

            public byte[] VideoData { get; set; }
        }

        public static async Task<MediaData> DownloadM3U8Async(string m3u8Url)
        {
            using HttpClient client = new();
            // Step 1: Download the master playlist content
            string masterPlaylistContent;
            try
            {
                masterPlaylistContent = await client.GetStringAsync(m3u8Url);
            }
            catch (Exception ex)
            {
                throw new Exception("Error downloading master playlist: " + ex.Message, ex);
            }

            // Step 2: Parse the master playlist to find video and audio playlists
            (string videoPlaylistUrl, string audioPlaylistUrl) = ParseMasterPlaylist(masterPlaylistContent, m3u8Url);

            if (string.IsNullOrEmpty(videoPlaylistUrl))
            {
                throw new Exception("No video playlist found in the master playlist.");
            }

            // Step 3: Download and parse the video playlist
            List<string> videoSegmentUrls = await GetSegmentUrlsAsync(client, videoPlaylistUrl);

            // Step 4: Download and parse the audio playlist (if available)
            List<string> audioSegmentUrls = null;
            if (!string.IsNullOrEmpty(audioPlaylistUrl))
            {
                audioSegmentUrls = await GetSegmentUrlsAsync(client, audioPlaylistUrl);
            }

            // Step 5: Download the video and audio segments
            MediaData mediaData = new()
            {
                VideoData = await DownloadSegmentsAsync(client, videoSegmentUrls)
            };

            if (audioSegmentUrls != null)
            {
                mediaData.AudioData = await DownloadSegmentsAsync(client, audioSegmentUrls);
            }

            return mediaData;
        }

        private static async Task<byte[]> DownloadSegmentsAsync(HttpClient client, List<string> segmentUrls)
        {
            using MemoryStream memoryStream = new();
            int segmentNumber = 0;
            foreach (string segmentUrl in segmentUrls)
            {
                segmentNumber++;
                try
                {
                    byte[] segmentData = await client.GetByteArrayAsync(segmentUrl);
                    await memoryStream.WriteAsync(segmentData, 0, segmentData.Length);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error downloading segment {segmentNumber}: {ex.Message}", ex);
                }
            }

            return memoryStream.ToArray();
        }

        private static string GetAbsoluteUrl(string baseUrl, string relativeUrl)
        {
            Uri baseUri = new(baseUrl);
            Uri absoluteUri = new(baseUri, relativeUrl);
            return absoluteUri.ToString();
        }

        private static string GetAttributeValue(string line, string attributeName)
        {
            string[] parts = line.Split(',');
            foreach (string part in parts)
            {
                string[] keyValue = part.Split(new char[] { '=' }, 2);
                if (keyValue.Length == 2 && keyValue[0].Trim().Equals(attributeName, StringComparison.OrdinalIgnoreCase))
                {
                    string value = keyValue[1].Trim();
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value[1..^1];
                    }

                    return value;
                }
            }

            return null;
        }

        private static async Task<List<string>> GetSegmentUrlsAsync(HttpClient client, string playlistUrl)
        {
            string playlistContent;
            try
            {
                playlistContent = await client.GetStringAsync(playlistUrl);
            }
            catch (Exception ex)
            {
                throw new Exception("Error downloading playlist: " + ex.Message, ex);
            }

            return ParseMediaPlaylist(playlistContent, playlistUrl);
        }

        private static (string videoPlaylistUrl, string audioPlaylistUrl) ParseMasterPlaylist(string masterPlaylistContent, string baseUrl)
        {
            string videoPlaylistUrl = null;
            string audioPlaylistUrl = null;

            StringReader reader = new(masterPlaylistContent);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("#EXT-X-MEDIA"))
                {
                    // Parse audio track
                    if (line.Contains("TYPE=AUDIO") && line.Contains("DEFAULT=YES"))
                    {
                        string uri = GetAttributeValue(line, "URI");
                        if (!string.IsNullOrEmpty(uri))
                        {
                            audioPlaylistUrl = GetAbsoluteUrl(baseUrl, uri);
                        }
                    }
                }
                else if (line.StartsWith("#EXT-X-STREAM-INF"))
                {
                    // Parse video track
                    // The next line should be the video playlist URL
                    string streamInfo = line;
                    string playlistUrl = reader.ReadLine();
                    if (!string.IsNullOrEmpty(playlistUrl))
                    {
                        string absoluteUrl = GetAbsoluteUrl(baseUrl, playlistUrl);

                        // For simplicity, select the first video playlist found
                        videoPlaylistUrl ??= absoluteUrl;
                    }
                }
            }

            return (videoPlaylistUrl, audioPlaylistUrl);
        }

        private static List<string> ParseMediaPlaylist(string playlistContent, string baseUrl)
        {
            List<string> segmentUrls = [];
            StringReader reader = new(playlistContent);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("#"))
                {
                    // Skip comments and tags
                    continue;
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    // This is a segment URL
                    string segmentUrl = GetAbsoluteUrl(baseUrl, line.Trim());
                    segmentUrls.Add(segmentUrl);
                }
            }

            return segmentUrls;
        }
    }
}