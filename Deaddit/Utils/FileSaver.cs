using CommunityToolkit.Maui.Storage;
using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.IO;
using Deaddit.Core.Utils.Validation;

namespace Deaddit.Utils
{
    public static class FileStorage
    {
        public static async Task Save(IEnumerable<FileDownload> items, IStreamConverter? converter = null, SavePathConfiguration? savePaths = null, CancellationToken cancellationToken = default)
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

            await SaveMultipleFiles(files, savePaths, cancellationToken);
        }

        public static async Task Save(FileDownload item, IStreamConverter? converter = null, SavePathConfiguration? savePaths = null, CancellationToken cancellationToken = default)
        {
            Ensure.NotNull(item);

            await Save([item], converter, savePaths, cancellationToken);
        }

        public static async Task SaveMultipleFiles(List<(string fileName, Func<Task<Stream>> fileStream)> files, SavePathConfiguration? savePaths = null, CancellationToken cancellationToken = default)
        {
            Ensure.NotNullOrEmpty(files);

            foreach ((string fileName, Func<Task<Stream>> fileStream) in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? targetDirectory = ResolveTargetDirectory(fileName, savePaths);

                if (targetDirectory != null)
                {
                    await WriteToDirectory(targetDirectory, fileName, fileStream, cancellationToken);
                }
                else
                {
                    await FileSaver.Default.SaveAsync(fileName, await fileStream(), cancellationToken);
                }
            }
        }

        private static string? ResolveTargetDirectory(string fileName, SavePathConfiguration? savePaths)
        {
            if (savePaths == null)
            {
                return null;
            }

            if (UrlHelper.IsImageFile(fileName) && !string.IsNullOrWhiteSpace(savePaths.DefaultImageDirectory))
            {
                return savePaths.DefaultImageDirectory;
            }

            if (UrlHelper.IsVideoFile(fileName) && !string.IsNullOrWhiteSpace(savePaths.DefaultVideoDirectory))
            {
                return savePaths.DefaultVideoDirectory;
            }

            return null;
        }

        private static async Task WriteToDirectory(string directory, string fileName, Func<Task<Stream>> fileStreamFactory, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(directory);

            string targetPath = Path.Combine(directory, fileName);

            await using Stream source = await fileStreamFactory();
            await using FileStream destination = File.Create(targetPath);
            await source.CopyToAsync(destination, cancellationToken);
        }
    }
}
