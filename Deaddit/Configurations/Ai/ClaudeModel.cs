using Deaddit.Json.Converters;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Deaddit.Configurations.Ai
{
    [JsonConverter(typeof(ClaudeModelJsonConverter))]
    public enum ClaudeModel
    {
        // Blank / unset. A saved configuration that references a model which has since been
        // removed from this list deserializes to None instead of crashing the app.
        [Description("")]
        None = 0,

        [Description("claude-opus-4-8")]
        Claude_Opus_4_8,

        [Description("claude-opus-4-7")]
        Claude_Opus_4_7,

        [Description("claude-opus-4-6")]
        Claude_Opus_4_6,

        [Description("claude-opus-4-5-20251101")]
        Claude_Opus_4_5,

        [Description("claude-opus-4-1-20250805")]
        Claude_Opus_4_1,

        [Description("claude-sonnet-4-6")]
        Claude_Sonnet_4_6,

        [Description("claude-sonnet-4-5-20250929")]
        Claude_Sonnet_4_5,

        [Description("claude-haiku-4-5-20251001")]
        Claude_Haiku_4_5
    }
}
