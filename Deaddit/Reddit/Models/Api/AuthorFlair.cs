﻿using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class AuthorFlair
    {
        [JsonPropertyName("a")]
        public string? Author { get; set; }

        [JsonPropertyName("e")]
        public string? Emoji { get; set; }

        [JsonPropertyName("u")]
        public string? UserLink { get; set; }
    }
}