using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class MoreCommentsResponse
    {
        [NotNull]
        [JsonPropertyName("json")]
        public MoreCommentsResponseMeta Json { get; init; }
    }
}