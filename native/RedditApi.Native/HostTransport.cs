using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedditApi.Native
{
    /// <summary>
    /// Sends every HTTP request Reddit.Api makes through the host app (OkHttp on Android).
    /// Natively compiled .NET on Android has no TLS library to use: Android doesn't expose one to
    /// apps. So .NET does all of Reddit.Api's logic and the host only moves bytes.
    /// </summary>
    internal sealed class HostTransport : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HostRequest hostRequest = new()
            {
                Method = request.Method.Method,
                Url = request.RequestUri!.AbsoluteUri,
                Headers = []
            };

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                hostRequest.Headers.Add([header.Key, string.Join(header.Key == "User-Agent" ? " " : ", ", header.Value)]);
            }

            if (request.Content != null)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
                {
                    hostRequest.Headers.Add([header.Key, string.Join(", ", header.Value)]);
                }

                hostRequest.BodyBase64 = Convert.ToBase64String(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            }

            string responseJson = Bridge.SendToHost(JsonSerializer.Serialize(hostRequest, BridgeJson.Default.HostRequest));
            HostResponse hostResponse = JsonSerializer.Deserialize(responseJson, BridgeJson.Default.HostResponse)
                ?? throw new InvalidOperationException("The host returned no response.");

            if (hostResponse.Error != null)
            {
                throw new HttpRequestException($"The host failed to send {hostRequest.Method} {hostRequest.Url}: {hostResponse.Error}");
            }

            HttpResponseMessage response = new((HttpStatusCode)hostResponse.Status)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(Convert.FromBase64String(hostResponse.BodyBase64 ?? string.Empty))
            };

            foreach (string[] header in hostResponse.Headers ?? [])
            {
                if (!response.Headers.TryAddWithoutValidation(header[0], header[1]))
                {
                    response.Content.Headers.TryAddWithoutValidation(header[0], header[1]);
                }
            }

            return response;
        }
    }

    internal sealed class HostRequest
    {
        public string Method { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public List<string[]> Headers { get; set; } = [];

        public string? BodyBase64 { get; set; }
    }

    internal sealed class HostResponse
    {
        public int Status { get; set; }

        public List<string[]>? Headers { get; set; }

        public string? BodyBase64 { get; set; }

        /// <summary>
        /// Set when the request couldn't be sent at all (no network, DNS, TLS).
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// What a failed call reports to the host.
    /// </summary>
    internal sealed class BridgeError
    {
        public string Type { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public int? Status { get; set; }

        public string? Url { get; set; }

        public List<string>? Codes { get; set; }

        /// <summary>
        /// The whole exception: type, message, inner exceptions and stack.
        /// </summary>
        public string Detail { get; set; } = string.Empty;
    }

    internal sealed class BridgeFailure
    {
        public BridgeError Error { get; set; } = new();
    }

    /// <summary>
    /// The bridge's own JSON, generated at compile time so none of it depends on reflection
    /// surviving native compilation.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(HostRequest))]
    [JsonSerializable(typeof(HostResponse))]
    [JsonSerializable(typeof(BridgeFailure))]
    [JsonSerializable(typeof(Reddit.Api.Models.BearerToken))]
    internal sealed partial class BridgeJson : JsonSerializerContext
    {
    }
}
