using System.Collections.Concurrent;

namespace Deaddit.Utils
{
    public class ETagCache
    {
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public void Cache(IEnumerable<string> urls)
        {
            Parallel.ForEach(urls, url => this.Get(url));
        }

        public string Get(string url)
        {
            if(!Uri.TryCreate(url, UriKind.Absolute, out Uri? _))
            {
                return string.Empty;
            }

            if (!_cache.TryGetValue(url, out string? etag))
            {
                HttpClient httpClient = new();

                HttpRequestMessage request = new(HttpMethod.Head, url);

                HttpResponseMessage response = httpClient.Send(request);

                etag = response.Headers.ETag?.Tag ?? string.Empty;
                _cache.TryAdd(url, etag);

            }

            return etag;
        }
    }
}
