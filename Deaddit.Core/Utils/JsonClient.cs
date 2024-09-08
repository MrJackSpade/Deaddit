using Deaddit.Core.Extensions;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Json;
using System.Diagnostics;
using System.Net.Http.Json;

namespace Deaddit.Core.Utils
{
    public class JsonClient : IJsonClient
    {
        private readonly HttpClient _httpClient;

        public JsonClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            this.SetDefaultHeader("Accept", "application/json");
        }

        public async Task<T> Get<T>(string url) where T : class
        {
            string response = await Retry(async () =>
            {
                HttpResponseMessage httpResponse = await _httpClient.GetAsync(url);
                string responseBody = await httpResponse.Content.ReadAsStringAsync();
                httpResponse.EnsureSuccessStatusCode();
                return responseBody;
            });

            return JsonDeserializer.Deserialize<T>(response);
        }

        public async Task<T> Post<T>(string url, object body) where T : class
        {
            JsonContent content = JsonContent.Create(body);

            HttpResponseMessage responseMessage = await _httpClient.PostAsync(url, content);

            string response = await responseMessage.Content.ReadAsStringAsync();

            responseMessage.EnsureSuccessStatusCode();

            return JsonDeserializer.Deserialize<T>(response);
        }

        public async Task Post(string url)
        {
            HttpResponseMessage responseMessage = await _httpClient.PostAsync(url, null);

            string response = await responseMessage.Content.ReadAsStringAsync();

            Debug.WriteLine(response);

            responseMessage.EnsureSuccessStatusCode();
        }

        public async Task Post(string url, object body)
        {
            JsonContent content = JsonContent.Create(body);

            HttpResponseMessage responseMessage = await _httpClient.PostAsync(url, content);

            string response = await responseMessage.Content.ReadAsStringAsync();

            Debug.WriteLine(response);

            responseMessage.EnsureSuccessStatusCode();
        }

        public async Task Post(string url, Dictionary<string, string> formValues)
        {
            // Create the content to be posted as form data
            FormUrlEncodedContent content = new(formValues);

            // Post the form data to the specified URL
            HttpResponseMessage responseMessage = await _httpClient.PostAsync(url, content);

            // Read the response content as a string
            string response = await responseMessage.Content.ReadAsStringAsync();

            Debug.WriteLine(response);

            // Ensure the response indicates success
            responseMessage.EnsureSuccessStatusCode();
        }

        public async Task<T> Post<T>(string url, Dictionary<string, string> formValues) where T : class
        {
            // Create the content to be posted as form data
            FormUrlEncodedContent content = new(formValues);

            // Post the form data to the specified URL
            HttpResponseMessage responseMessage = await _httpClient.PostAsync(url, content);

            // Read the response content as a string
            string response = await responseMessage.Content.ReadAsStringAsync();

            // Ensure the response indicates success
            responseMessage.EnsureSuccessStatusCode();

            return JsonDeserializer.Deserialize<T>(response);
        }

        public void SetDefaultHeader(string key, string value)
        {
            _httpClient.SetDefaultHeader(key, value);
        }

        private static T Retry<T>(Func<T> func, int maxRetries = 3)
        {
            int retries = 0;

            while (true)
            {
                try
                {
                    return func();
                }
                catch (HttpRequestException e) when (e.StatusCode is System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.NotFound)
                {
                    if (retries++ > maxRetries)
                    {
                        throw;
                    }

                    Thread.Sleep(retries++ * 1000);

                    Debug.WriteLine(e.Message);
                }
            }
        }
    }
}