using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Utils;
using System.Net.Http.Json;

namespace Deaddit.Services
{
    internal class JsonClient : IJsonClient
    {
        private readonly HttpClient _httpClient;

        public JsonClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            this.SetDefaultHeader("Accept", "application/json");
        }

        public async Task<T> Get<T>(string url) where T : class, new()
        {
            string response = await _httpClient.GetStringAsync(url);

            return JsonDeserializer.Deserialize<T>(response);
        }

        public async Task<T> Post<T>(string url, object body) where T : class, new()
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

            responseMessage.EnsureSuccessStatusCode();
        }

        public async Task Post(string url, object body)
        {
            JsonContent content = JsonContent.Create(body);

            HttpResponseMessage responseMessage = await _httpClient.PostAsync(url, content);

            string response = await responseMessage.Content.ReadAsStringAsync();

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

            // Ensure the response indicates success
            responseMessage.EnsureSuccessStatusCode();
        }

        public async Task<T> Post<T>(string url, Dictionary<string, string> formValues) where T : class, new()
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
    }
}