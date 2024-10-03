using CommunityToolkit.Maui.Storage;
using Deaddit.Core.Models;
using Deaddit.Core.Utils;

namespace Deaddit.Utils
{
    public static class FileStorage
    {
        public static async Task Save(IEnumerable<FileDownload> items)
        {
            List<(string fileName, Func<Task<Stream>> fileStream)> files = [];

            foreach (FileDownload item in items)
            {
                files.Add((item.FileName, new Func<Task<Stream>>(async () => await new HttpClient().GetStreamAsync(item.DownloadUrl))));
            }

            await SaveMultipleFiles(files);
        }

        public static async Task Save(FileDownload item)
        {
            Ensure.NotNull(item);

            await Save([item]);
        }

        public static async Task SaveMultipleFiles(List<(string fileName, Func<Task<Stream>> fileStream)> files)
        {
            Ensure.NotNullOrEmpty(files);

            foreach ((string fileName, Func<Task<Stream>> fileStream) in files)
            {
                await FileSaver.Default.SaveAsync(fileName, await fileStream(), CancellationToken.None);
            }
        }
    }
}