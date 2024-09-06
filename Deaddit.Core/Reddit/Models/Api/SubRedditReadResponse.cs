using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class SubRedditReadResponse
    {
        [JsonPropertyName("kind")]
        public ThingKind Kind { get; init; }

        [NotNull]
        [JsonPropertyName("data")]
        public ReadMetaData<ApiPostMeta> Meta { get; init; }
    }
}