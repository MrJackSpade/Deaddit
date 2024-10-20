using Deaddit.Core.Extensions;
using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models.Requests
{
    public class ApiEndpointDefinition
    {
        public string Url { get; private set; }

        public ApiEndpointDefinition(string url)
        {
            Url = url;

            if (!Url.StartsWith('/') && !Url.Contains("://"))
            {
                Url = "/" + Url;
            }
        }

        public ApiEndpointDefinition WithCallingUser(string user)
        {
            return new ApiEndpointDefinition(Url.Replace("%USER%", user));
        }

        public ApiEndpointDefinition WithParam(string key, object? value)
        {
            return this.WithParams(new Dictionary<string, object?> { { key, value } });
        }

        public ApiEndpointDefinition WithParams(Dictionary<string, object?> parameters)
        {
            if (parameters.Count == 0)
            {
                return this;
            }

            if (Url.Contains('?'))
            {
                return new ApiEndpointDefinition(Url + "&" + string.Join("&", parameters.Select(static x => $"{x.Key}={x.Value}")));
            }
            else
            {
                return new ApiEndpointDefinition(Url + "?" + string.Join("&", parameters.Select(static x => $"{x.Key}={x.Value}")));
            }
        }

        public ApiEndpointDefinition WithRoot(string root)
        {
            if (Url.Contains("://"))
            {
                Uri uri = new(Url);
                return new ApiEndpointDefinition($"{root}{uri.PathAndQuery}");
            }
            else
            {
                return new ApiEndpointDefinition($"{root}{Url}");
            }
        }

        public ApiEndpointDefinition WithSort(Enum sort)
        {
            string sortString = GetSortString(sort);

            if (Url.Contains('?'))
            {
                string parameters = Url[(Url.IndexOf('?') + 1)..];
                string root = Url[..Url.IndexOf('?')];

                return new ApiEndpointDefinition($"{root}{sortString}?{parameters}");
            }
            else
            {
                return new ApiEndpointDefinition($"{Url}{sortString}");
            }
        }

        private static string GetSortString(Enum sort)
        {
            if (sort == null)
            {
                return string.Empty;
            }

            string sortString;

            if (sort.GetAttribute<EnumMemberAttribute>() is EnumMemberAttribute ema && string.IsNullOrWhiteSpace(ema.Value))
            {
                return string.Empty;
            }
            else
            {
                sortString = sort.ToString().ToLower();
            }

            if (sortString.Length > 0 && sortString[0] != '/')
            {
                sortString = $"/{sortString}";
            }

            return sortString;
        }
    }
}