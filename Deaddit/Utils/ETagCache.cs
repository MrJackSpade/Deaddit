using System.Collections.Concurrent;

namespace Deaddit.Utils
{
    public class ETagCache
    {
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public async Task Cache(IEnumerable<string> urls)
        {
            await Parallel.ForEachAsync(urls, async (url, c) => await this.Get(url));
        }

        public async Task<string> Get(string url)
        {
            if(!Uri.TryCreate(url, UriKind.Absolute, out Uri? _))
            {
                return string.Empty;
            }

            if (!_cache.TryGetValue(url, out string? etag))
            {
                HttpClient httpClient = new();

                HttpRequestMessage request = new(HttpMethod.Head, url);

                HttpResponseMessage response = await httpClient.SendAsync(request);

                etag = response.Headers.ETag?.Tag ?? string.Empty;

                _cache.TryAdd(url, etag);
            }

            return etag;
        }
    }
}
