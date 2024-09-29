using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Maui.WebComponents.Classes
{
    class OperationResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
