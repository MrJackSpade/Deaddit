﻿using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class Item
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("media_id")]
        public string? MediaId { get; init; }
    }
}