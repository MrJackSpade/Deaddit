using Deaddit.Core.Interfaces;
using Deaddit.Core.Models;
using Deaddit.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Deaddit.Handlers.Url
{
    class RedGifsHandler(IAppNavigator appNavigator) : IUrlHandler
    {
        private readonly IAppNavigator _appNavigator = appNavigator;

        public bool CanLaunch(string url, IAggregatePostHandler? aggregatePostHandler)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Host.EndsWith("redgifs.com", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> Exists(string url)
        {
            bool exists = await Task.Run(async () =>
            {
                using var client = new HttpClient();

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Head, url);
                    var response = await client.SendAsync(request);

                    return response.IsSuccessStatusCode;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine("Request error: " + ex.Message);
                }

                return false;
            });

            return exists;
        }
    

        public async Task Launch(string url, IAggregatePostHandler aggregatePostHandler)
        {
            string name = url.Split('/').Last();

            if(name.Contains('?'))
            {
                name = name.Split('?').First();
            }

            string source = string.Empty;

            await Task.Run(() => source = new System.Net.WebClient().DownloadString(url));

            string properlyCapitalized = string.Empty;

            foreach(string chunk in source.Split("\"").Skip(1))
            {
                if(!Uri.TryCreate(chunk, UriKind.Absolute, out Uri? thisUrl))
                {
                    continue;
                }

                if (chunk.Contains("files.redgifs.com", StringComparison.OrdinalIgnoreCase) && chunk.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    int indexOfName = chunk.IndexOf(name, StringComparison.OrdinalIgnoreCase);
                    properlyCapitalized = chunk.Substring(indexOfName, name.Length);
                    break;
                }
            }

            List<string> newUrls = [
                $"https://files.redgifs.com/{properlyCapitalized}.mp4",
                $"https://files.redgifs.com/{properlyCapitalized}-mobile.mp4",
            ];

            string? newUrl = string.Empty;

            foreach(string thisUrl in newUrls)
            {
                if (await this.Exists(thisUrl))
                {
                    newUrl = thisUrl;
                    break;
                }
            }

            await _appNavigator.OpenVideo(new FileDownload(newUrl));
        }
    }
}
