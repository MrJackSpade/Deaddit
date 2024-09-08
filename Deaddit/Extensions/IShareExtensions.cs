using Deaddit.Core.Reddit.Models;

namespace Deaddit.Extensions
{
    internal static class IShareExtensions
    {
        public static string GetFileName(string uri)
        {
            string fname = Path.GetFileName(uri);

            if (fname.Contains('?'))
            {
                fname = fname[..fname.IndexOf('?')];
            }

            if (string.IsNullOrWhiteSpace(fname))
            {
                fname = "file";
            }

            return fname;
        }

        public static async Task ShareFiles(this IShare share, string title, PostItems items)
        {
            List<ShareFile> files = [];
            HttpClient client = new();

            foreach (PostItem uri in items)
            {
                string file = Path.Combine(FileSystem.CacheDirectory, uri.FileName);
                byte[] fileBytes = await client.GetByteArrayAsync(uri.DownloadUrl);
                await File.WriteAllBytesAsync(file, fileBytes);
                files.Add(new ShareFile(file));
            }

            await share.RequestAsync(new ShareMultipleFilesRequest
            {
                Title = title,
                Files = files
            });
        }
    }
}