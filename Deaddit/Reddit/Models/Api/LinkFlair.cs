﻿using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class LinkFlair
    {
        [JsonPropertyName("a")]
        public string? A { get; set; }

        [JsonPropertyName("t")]
        public string? Text { get; set; }

        [JsonPropertyName("e")]
        public string? Type { get; set; }

        [JsonPropertyName("u")]
        public string? U { get; set; }
    }
}