using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Deaddit.Models.Json.Response
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