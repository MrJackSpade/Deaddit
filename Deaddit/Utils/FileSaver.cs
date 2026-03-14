using CommunityToolkit.Maui.Storage;
using Deaddit.Core.Models;
using Deaddit.Core.Utils.IO;
using Deaddit.Core.Utils.Validation;

namespace Deaddit.Utils
{
    public static class FileStorage
    {
        public static async Task Save(IEnumerable<FileDownload> items, CancellationToken cancellationToken = default)
        {
            List<(string fileName, Func<Task<Stream>> fileStream)> files = [];

            foreach (FileDownload item in items)
            {
                files.Add((item.FileName, new Func<Task<Stream>>(async () => await FileStreamService.GetStream(item.DownloadUrl))));
            }

            await SaveMultipleFiles(files, cancellationToken);
        }

        public static async Task Save(FileDownload item, CancellationToken cancellationToken = default)
        {
            Ensure.NotNull(item);

            await Save([item], cancellationToken);
        }

        public static async Task SaveMultipleFiles(List<(string fileName, Func<Task<Stream>> fileStream)> files, CancellationToken cancellationToken = default)
        {
            Ensure.NotNullOrEmpty(files);

            foreach ((string fileName, Func<Task<Stream>> fileStream) in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await FileSaver.Default.SaveAsync(fileName, await fileStream(), cancellationToken);
            }
        }
    }
}