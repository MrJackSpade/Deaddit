using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Extensions
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