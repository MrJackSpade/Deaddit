using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class MoreCommentsData
    {
        [JsonPropertyName("things")]
        public List<ApiThing> Things { get; init; } = [];
    }
}