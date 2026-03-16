using CommunityToolkit.Maui.Storage;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Utils.IO;
using Deaddit.Core.Utils.Validation;

namespace Deaddit.Utils
{
    public static class FileStorage
    {
        public static async Task Save(IEnumerable<FileDownload> items, IStreamConverter? converter = null, CancellationToken cancellationToken = default)
        {
            List<(string fileName, Func<Task<Stream>> fileStream)> files = [];

            foreach (FileDownload item in items)
            {
                string fileName = item.FileName;

                if (converter != null && converter.CanConvert(fileName))
                {
                    fileName = converter.ConvertFileName(fileName);
                    files.Add((fileName, async () =>
                    {
                        Stream source = await FileStreamService.GetStream(item.DownloadUrl);
                        return await converter.ConvertAsync(source);
                    }));
                }
                else
                {
                    files.Add((fileName, async () => await FileStreamService.GetStream(item.DownloadUrl)));
                }
            }

            await SaveMultipleFiles(files, cancellationToken);
        }

        public static async Task Save(FileDownload item, IStreamConverter? converter = null, CancellationToken cancellationToken = default)
        {
            Ensure.NotNull(item);

            await Save([item], converter, cancellationToken);
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