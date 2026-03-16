using Reddit.Api.Models.Json.Listings;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class Variants
    {
        public string? Gif { get; init; }

        public string? MP4 { get; init; }

        public RemoteImage? Nsfw { get; init; }

        public Media? Obfuscated { get; init; }
    }
}