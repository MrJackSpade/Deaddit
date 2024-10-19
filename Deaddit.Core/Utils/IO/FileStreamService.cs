using Deaddit.Core.Interfaces;

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
                return new MemoryStream((await M3U8Downloader.DownloadM3U8Async(url)).VideoData);
            }
            else
            {
                return await new HttpClient().GetStreamAsync(url);
            }
        }
    }
}