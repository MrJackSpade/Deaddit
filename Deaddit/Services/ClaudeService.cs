using Deaddit.Configurations;
using Deaddit.Configurations.Ai;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Services.Models;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deaddit.Services
{
    public class ClaudeService : IClaudeService
    {
        private readonly string _apiKey;

        private readonly HttpClient _httpClient;

        private readonly JsonSerializerOptions _jsonOptions;

        private readonly string _messageUrl = "https://api.anthropic.com/v1/messages";

        private readonly string _tokenUrl = "https://api.anthropic.com/v1/messages/count_tokens";

        public ClaudeService(AIConfiguration aiConfiguration)
        {
            _apiKey = string.IsNullOrWhiteSpace(aiConfiguration.ApiKey) ? throw new ArgumentNullException(nameof(aiConfiguration.ApiKey)) : aiConfiguration.ApiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new MessageConverter() }
            };
        }

        public async Task<int> CountTokens(string prompt, string input, string? prefill = null)
        {
            List<Message> messages =
            [
                new UserMessage(input)
            ];

            if (!string.IsNullOrWhiteSpace(prefill))
            {
                messages.Add(new AssistantMessage(prefill));
            }

            ClaudeTokenResponse? response = await this.CountTokens(messages, systemPrompt: prompt);

            if (response is null)
            {
                throw new InvalidOperationException();
            }

            return response.InputTokens;
        }

        public async Task<ClaudeTokenResponse?> CountTokens(List<Message> messages,
                                                         string? systemPrompt = null,
                                                         string? model = null)
        {
            ClaudeTokenRequest request = new()
            {
                Model = model ?? ClaudeModel.Claude_Sonnet_4_5.ToModelId(),
                Messages = messages,
                System = systemPrompt,
            };

            string json = JsonSerializer.Serialize(request, _jsonOptions);
            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(_tokenUrl, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Claude API request failed: {response.StatusCode} - {responseBody}");
            }

            return JsonSerializer.Deserialize<ClaudeTokenResponse>(responseBody, _jsonOptions);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public async Task<string> SendMessageAsync(string prompt, string input, string? model = null, string? prefill = null, double temperature = 0)
        {
            List<Message> messages =
            [
                new UserMessage(input)
            ];

            if (!string.IsNullOrWhiteSpace(prefill))
            {
                messages.Add(new AssistantMessage(prefill));
            }

            ClaudeMessageResponse? response = await this.SendMessageAsync(messages, systemPrompt: prompt, model: model, temperature: temperature);

            string responseString = response.Content.FirstOrDefault()?.Text ?? string.Empty;

            if (prefill != null)
            {
                responseString = prefill + responseString;
            }

            return responseString;
        }

        public async Task<string> SendMessageAsync(string input, double temperature = 0)
        {
            List<Message> messages = new()
            {
                new UserMessage(input)
            };

            ClaudeMessageResponse? response = await this.SendMessageAsync(messages, temperature: temperature);
            return response.Content.FirstOrDefault()?.Text ?? string.Empty;
        }

        public async Task<ClaudeMessageResponse?> SendMessageAsync(List<Message> messages,
                                                           string? systemPrompt = null,
                                                           string? model = null,
                                                           int maxTokens = 10000,
                                                           double? temperature = 0,
                                                           int maxRetries = 3)
        {
            ClaudeMessageRequest request = new()
            {
                Model = model ?? ClaudeModel.Claude_Sonnet_4_5.ToModelId(),
                MaxTokens = maxTokens,
                Messages = messages,
                System = systemPrompt,
                Temperature = temperature
            };

            string json = JsonSerializer.Serialize(request, _jsonOptions);

            int retryCount = 0;
            int baseDelayMs = 1000; // Start with 1 second

            while (retryCount <= maxRetries)
            {
                try
                {
                    StringContent content = new(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _httpClient.PostAsync(_messageUrl, content);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Check for retryable status codes
                    if (IsRetryableStatusCode(response.StatusCode))
                    {
                        if (retryCount < maxRetries)
                        {
                            retryCount++;
                            int delayMs = baseDelayMs * (int)Math.Pow(2, retryCount - 1); // Exponential backoff

                            // Add jitter to prevent thundering herd
                            int jitter = Random.Shared.Next(0, 1000);
                            delayMs += jitter;

                            Console.WriteLine($"Retryable error {response.StatusCode}. Retry {retryCount}/{maxRetries} after {delayMs}ms delay.");
                            await Task.Delay(delayMs);
                            continue;
                        }
                        else
                        {
                            throw new HttpRequestException($"Claude API request failed after {maxRetries} retries: {response.StatusCode} - {responseBody}");
                        }
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Claude API request failed: {response.StatusCode} - {responseBody}");
                    }

                    return JsonSerializer.Deserialize<ClaudeMessageResponse>(responseBody, _jsonOptions);
                }
                catch (HttpRequestException) when (retryCount < maxRetries)
                {
                    // Network-level errors (connection issues, timeouts)
                    retryCount++;
                    int delayMs = baseDelayMs * (int)Math.Pow(2, retryCount - 1);
                    int jitter = Random.Shared.Next(0, 1000);
                    delayMs += jitter;

                    Console.WriteLine($"Network error. Retry {retryCount}/{maxRetries} after {delayMs}ms delay.");
                    await Task.Delay(delayMs);
                }
            }

            throw new HttpRequestException($"Claude API request failed after {maxRetries} retries");
        }

        private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.BadGateway ||           // 502
                   statusCode == HttpStatusCode.ServiceUnavailable ||   // 503
                   statusCode == HttpStatusCode.GatewayTimeout ||       // 504
                   (int)statusCode == 529;                              // Overloaded (custom status code)
        }
    }
}
