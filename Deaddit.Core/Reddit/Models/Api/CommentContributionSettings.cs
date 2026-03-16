
namespace Deaddit.Core.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class CommentContributionSettings
    {
        public List<string> AllowedMediaTypes { get; init; } = [];
    }
}