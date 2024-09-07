using Deaddit.Core.Reddit.Models;
using System.Net;

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

            Parallel.ForEach(items, (uri) =>
            {
                string file = Path.Combine(FileSystem.CacheDirectory, uri.FileName);
                new WebClient().DownloadFile(uri.DownloadUrl, file);
                files.Add(new ShareFile(file));
            });

            await Share.Default.RequestAsync(new ShareMultipleFilesRequest
            {
                Title = title,
                Files = files
            });
        }
    }
}