using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiMulti
    {
        [JsonPropertyName("can_edit")]
        public bool CanEdit { get; set; }

        [JsonPropertyName("copied_from")]
        public string? CopiedFrom { get; set; }

        [JsonPropertyName("created")]
        public double Created { get; set; }

        [JsonPropertyName("created_utc")]
        public OptionalDateTime CreatedUtc { get; set; }

        [JsonPropertyName("description_html")]
        public string? DescriptionHtml { get; set; }

        [JsonPropertyName("description_md")]
        public string? DescriptionMd { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("is_favorited")]
        public bool IsFavorited { get; set; }

        [JsonPropertyName("is_subscriber")]
        public bool IsSubscriber { get; set; }

        [JsonPropertyName("key_color")]
        public object? KeyColor { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("num_subscribers")]
        public int NumSubscribers { get; set; }

        [JsonPropertyName("over_18")]
        public bool Over18 { get; set; }

        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("owner_id")]
        public string? OwnerId { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("subreddits")]
        public List<ApiMultiSubReddit> Subreddits { get; set; } = [];

        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; }
    }

    public class ApiMultiMeta
    {
        [NotNull]
        [JsonPropertyName("data")]
        public ApiMulti Data { get; set; }

        [JsonPropertyName("kind")]
        public ThingKind Kind { get; set; }
    }

    public class ApiMultiSubReddit
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}