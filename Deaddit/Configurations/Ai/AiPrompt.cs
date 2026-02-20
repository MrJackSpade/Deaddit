using Deaddit.Core.Attributes;

namespace Deaddit.Configurations.Ai
{
    public class AiPrompt
    {
        [EditorDisplay(Name = "Display Name", Description = "The name of the prompt as shown in the UI")]
        public string DisplayName { get; set; } = string.Empty;

        [EditorDisplay(Name = "Model", Description = "The Claude model to use for this prompt")]
        public ClaudeModel Model { get; set; } = ClaudeModel.Claude_Sonnet_4_5;

        [EditorDisplay(Name = "Text Content", Description = "The prompt text to be sent to the AI model. Use {0} to inject history and {1} to inject a custom user command", Multiline = true)]
        public string TextContent { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }
}