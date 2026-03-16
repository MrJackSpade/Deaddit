
namespace Deaddit.Core.Reddit.Models
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class PostMessageResponse
    {
        public PostMessageResponseMeta Json { get; init; }
    }

    public class PostMessageResponseMeta
    {
        public List<string> Errors { get; init; } = [];
    }
}