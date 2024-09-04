using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Deaddit.Reddit.Models.Api
{

    public class SubRedditAboutResponse
    {
        [JsonPropertyName("kind")]
        public ThingKind? Kind { get; set; }

        [JsonPropertyName("data")]
        public ApiSubReddit Data { get; set; }
    }
}
