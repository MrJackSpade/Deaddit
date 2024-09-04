﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class SubRedditReadResponse
    {
        [JsonPropertyName("kind")]
        public ThingKind Kind { get; init; }

        [NotNull]
        [JsonPropertyName("data")]
        public ReadMetaData<RedditPostMeta> Meta { get; init; }
    }
}