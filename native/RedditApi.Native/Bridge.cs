using Reddit.Api.Client;
using Reddit.Api.Exceptions;
using Reddit.Api.Models;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedditApi.Native
{
    /// <summary>
    /// The C functions the host app calls. Every call and result is UTF-8 JSON, allocated with the C
    /// heap: the host frees what .NET returns with free(), and .NET frees what the host returns.
    /// </summary>
    public static unsafe class Bridge
    {
        private static delegate* unmanaged<byte*, byte*> _transport;

        private static delegate* unmanaged<byte*> _refreshSession;

        private static RedditClient? _client;

        /// <summary>
        /// The JSON shape of Reddit.Api's models: the names Reddit uses.
        /// </summary>
        internal static readonly JsonSerializerOptions ModelJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = Reddit.Api.Json.RedditJson.TypeInfoResolver
        };

        /// <summary>
        /// Sets up the client. <paramref name="transport"/> sends one HTTP request (JSON in, JSON out);
        /// <paramref name="refreshSession"/> asks the user to log in again and returns the new session
        /// as JSON, or null when they don't. <paramref name="userAgent"/> null keeps Reddit.Api's default.
        /// </summary>
        /// <returns>Null on success; otherwise the error as JSON, for the host to throw.</returns>
        [UnmanagedCallersOnly(EntryPoint = "ra_init")]
        public static byte* Init(delegate* unmanaged<byte*, byte*> transport, delegate* unmanaged<byte*> refreshSession, byte* userAgent)
        {
            // An exception escaping an export aborts the process, with the message on stderr, which
            // Android discards. So every export reports its errors instead.
            try
            {
                _transport = transport;
                _refreshSession = refreshSession;

                HttpClient httpClient = new(new HostTransport());
                RedditCredentials credentials = new();

                // Null keeps Reddit.Api's default user agent.
                if (userAgent != null)
                {
                    credentials.UserAgent = Marshal.PtrToStringUTF8((IntPtr)userAgent)!;
                }

                _client = new RedditClient(credentials, httpClient);
                _client.SetTokenRefreshFunction(RefreshSessionAsync);
                return null;
            }
            catch (Exception ex)
            {
                return ToNative(FailureJson(ex));
            }
        }

        /// <summary>
        /// Runs one command: {"method": "...", "args": {...}}. Returns {"result": ...} or
        /// {"error": {...}}. Blocks until done, so the host calls it off its UI thread.
        /// </summary>
        [UnmanagedCallersOnly(EntryPoint = "ra_call")]
        public static byte* Call(byte* requestJson)
        {
            string request = Marshal.PtrToStringUTF8((IntPtr)requestJson)!;

            string response;

            try
            {
                if (_client == null)
                {
                    throw new InvalidOperationException("ra_init was not called.");
                }

                using JsonDocument document = JsonDocument.Parse(request);
                string method = document.RootElement.GetProperty("method").GetString()!;
                JsonElement args = document.RootElement.TryGetProperty("args", out JsonElement a) ? a : default;

                object? result = Task.Run(() => Commands.RunAsync(_client, method, args)).GetAwaiter().GetResult();
                response = "{\"result\":" + JsonSerializer.Serialize(result, ModelJson) + "}";
            }
            catch (Exception ex)
            {
                response = FailureJson(ex);
            }

            return ToNative(response);
        }

        [UnmanagedCallersOnly(EntryPoint = "ra_free")]
        public static void Free(byte* pointer)
        {
            NativeMemory.Free(pointer);
        }

        internal static string SendToHost(string requestJson)
        {
            byte* request = ToNative(requestJson);

            try
            {
                byte* response = _transport(request);
                return TakeString(response) ?? throw new InvalidOperationException("The host transport returned nothing.");
            }
            finally
            {
                NativeMemory.Free(request);
            }
        }

        private static Task<BearerToken?> RefreshSessionAsync()
        {
            return Task.Run(() =>
            {
                string? json = TakeString(_refreshSession());
                return json == null ? null : JsonSerializer.Deserialize(json, BridgeJson.Default.BearerToken);
            });
        }

        private static string FailureJson(Exception ex)
        {
            Exception inner = ex is AggregateException { InnerExceptions.Count: 1 } aggregate ? aggregate.InnerExceptions[0] : ex;

            BridgeFailure failure = new()
            {
                Error = new BridgeError
                {
                    Type = inner.GetType().Name,
                    Message = inner.Message,
                    Status = (inner as RedditHttpException)?.StatusCode is { } status ? (int)status : null,
                    Url = (inner as RedditHttpException)?.Url,
                    Codes = (inner as RedditApiException)?.Errors.Select(e => e.Code).ToList(),
                    Detail = inner.ToString(),
                    // Interpolated as the C# app's dialog did ($"{ex.StackTrace}"): null writes nothing.
                    StackTrace = $"{inner.StackTrace}"
                }
            };

            return JsonSerializer.Serialize(failure, BridgeJson.Default.BridgeFailure);
        }

        private static byte* ToNative(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte* pointer = (byte*)NativeMemory.Alloc((nuint)bytes.Length + 1);
            Marshal.Copy(bytes, 0, (IntPtr)pointer, bytes.Length);
            pointer[bytes.Length] = 0;
            return pointer;
        }

        /// <summary>
        /// Reads a string the host allocated, and frees it.
        /// </summary>
        private static string? TakeString(byte* pointer)
        {
            if (pointer == null)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUTF8((IntPtr)pointer);
            }
            finally
            {
                NativeMemory.Free(pointer);
            }
        }
    }
}
