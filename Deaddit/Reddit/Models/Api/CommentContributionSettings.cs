﻿using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class CommentContributionSettings
    {
        [JsonPropertyName("allowed_media_types")]
        public List<string> AllowedMediaTypes { get; set; } = [];
    }
}
