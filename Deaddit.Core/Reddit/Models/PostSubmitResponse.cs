
namespace Deaddit.Core.Reddit.Models
{
    public class Json
    {
        public PostSubmitData Data { get; set; }

        public List<string> Errors { get; set; }
    }

    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class PostSubmitData
    {
        public int DraftsCount { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }
    }

    public class PostSubmitResponse
    {
        public Json Json { get; set; }
    }
}