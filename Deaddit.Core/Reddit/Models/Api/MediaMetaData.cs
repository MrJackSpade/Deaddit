﻿using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class MediaMetaData
    {
        [JsonPropertyName("e")]
        public string? E { get; init; }

        [JsonPropertyName("ext")]
        public string? Ext { get; init; }

        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("m")]
        public string? M { get; init; }

        [JsonPropertyName("p")]
        public List<ImageLocation> Previews { get; init; } = [];

        [JsonPropertyName("s")]
        public ImageLocation? Source { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("t")]
        public string? Text { get; init; }
    }
}