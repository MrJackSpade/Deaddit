using Deaddit.Core.Utils.Validation;

namespace Deaddit.Core.Models
{
    public class FileDownload
    {
        public string DownloadUrl { get; set; }

        public string FileName { get; set; }

        public string LaunchUrl { get; set; }

        public FileDownload(string? fileName, string? launchUrl, string? downloadUrl)
        {
            Ensure.NotNullOrWhiteSpace(fileName);
            Ensure.NotNullOrWhiteSpace(launchUrl);
            Ensure.NotNullOrWhiteSpace(downloadUrl);

            FileName = fileName;
            LaunchUrl = launchUrl;
            DownloadUrl = downloadUrl;
        }

        public FileDownload(string fileName, string url)
        {
            FileName = fileName;
            LaunchUrl = url;
            DownloadUrl = url;
        }

        public FileDownload(string url)
        {
            FileName = Path.GetFileName(url);
            LaunchUrl = url;
            DownloadUrl = url;

            if (FileName.Contains('?'))
            {
                FileName = FileName[..FileName.IndexOf('?')];
            }
        }
    }
}