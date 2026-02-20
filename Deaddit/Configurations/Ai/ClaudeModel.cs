using System.ComponentModel;

namespace Deaddit.Configurations.Ai
{
    public enum ClaudeModel
    {
        [Description("claude-sonnet-4-6")]
        Claude_Sonnet_4_6,

        [Description("claude-opus-4-6")]
        Claude_Opus_4_6,

        [Description("claude-opus-4-5-20251101")]
        Claude_Opus_4_5,

        [Description("claude-haiku-4-5-20251001")]
        Claude_Haiku_4_5,

        [Description("claude-sonnet-4-5-20250929")]
        Claude_Sonnet_4_5,

        [Description("claude-opus-4-1-20250805")]
        Claude_Opus_4_1,

        [Description("claude-opus-4-20250514")]
        Claude_Opus_4,

        [Description("claude-sonnet-4-20250514")]
        Claude_Sonnet_4,

        [Description("claude-3-haiku-20240307")]
        Claude_Haiku_3
    }
}
