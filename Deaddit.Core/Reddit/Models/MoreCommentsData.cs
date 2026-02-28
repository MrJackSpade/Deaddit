using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public class MoreCommentsData
    {
        [JsonPropertyName("things")]
        public List<ApiThing> Things { get; init; } = [];
    }
}
