using CommunityToolkit.Maui.Storage;
using Deaddit.Core.Reddit.Models;

namespace Deaddit.Utils
{
    public static class FileStorage
    {

        public static async Task SaveMultipleFiles(List<(string fileName, Func<Task<Stream>> fileStream)> files)
        {
            foreach ((string fileName, Func<Task<Stream>> fileStream) in files)
            {
                await FileSaver.Default.SaveAsync(fileName, await fileStream(), CancellationToken.None);
            }
        }

        public static async Task Save(PostItems items)
        {
            List<(string fileName, Func<Task<Stream>> fileStream)> files = [];

            foreach (PostItem item in items)
            {
                files.Add((item.FileName, new Func<Task<Stream>>(async () => await new HttpClient().GetStreamAsync(item.DownloadUrl))));
            }

            await SaveMultipleFiles(files);
        }
    }
}
