namespace Deaddit.Core.Extensions
{
    public static class HttpClientExtensions
    {
        public static void SetDefaultHeader(this HttpClient client, string key, string value)
        {
            if (client.DefaultRequestHeaders.Contains(key))
            {
                client.DefaultRequestHeaders.Remove(key);
            }

            client.DefaultRequestHeaders.Add(key, value);
        }
    }
}