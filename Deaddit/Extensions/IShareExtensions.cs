using System.Net;

namespace Deaddit.Extensions
{
    internal static class IShareExtensions
    {
        public static async Task ShareFiles(this IShare share, string title, params string[] uris)
        {
            List<ShareFile> files = [];

            Parallel.ForEach(uris, (uri) =>
            {
                string file = Path.Combine(FileSystem.CacheDirectory, Path.GetFileName(uri));
                new WebClient().DownloadFile(uri, file);
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
