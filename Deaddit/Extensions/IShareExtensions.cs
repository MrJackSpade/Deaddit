using Deaddit.Core.Interfaces;
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

        public static async Task ShareFiles(this IShare share, string title, FileDownload item, IStreamConverter? converter = null)
        {
            await share.ShareFiles(title, [item], converter);
        }

        public static async Task ShareFiles(this IShare share, string title, IEnumerable<FileDownload> items, IStreamConverter? converter = null)
        {
            List<ShareFile> files = [];

            foreach (FileDownload item in items)
            {
                string fileName = item.FileName;
                Stream stream = await FileStreamService.GetStream(item.DownloadUrl);

                if (converter != null && converter.CanConvert(fileName))
                {
                    fileName = converter.ConvertFileName(fileName);
                    stream = await converter.ConvertAsync(stream);
                }

                string file = Path.Combine(FileSystem.CacheDirectory, fileName);

                if (stream is MemoryStream ms)
                {
                    await File.WriteAllBytesAsync(file, ms.ToArray());
                }
                else
                {
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