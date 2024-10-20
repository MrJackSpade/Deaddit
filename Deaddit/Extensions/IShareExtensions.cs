using Deaddit.Core.Models;
using Deaddit.Core.Utils.IO;

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

        public static async Task ShareFiles(this IShare share, string title, FileDownload item)
        {
            await share.ShareFiles(title, [item]);
        }

        public static async Task ShareFiles(this IShare share, string title, IEnumerable<FileDownload> items)
        {
            List<ShareFile> files = [];
            HttpClient client = new();

            foreach (FileDownload item in items)
            {
                string file = Path.Combine(FileSystem.CacheDirectory, item.FileName);
                Stream stream = await FileStreamService.GetStream(item.DownloadUrl);

                if (stream is MemoryStream ms)
                {
                    await File.WriteAllBytesAsync(file, ms.ToArray());
                }
                else
                {
                    //Off the main thread for android
                    await Task.Run(async () =>
                    {
                        using MemoryStream memoryStream = new();
                        stream.CopyTo(memoryStream);
                        await File.WriteAllBytesAsync(file, memoryStream.ToArray());
                    });
                }

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