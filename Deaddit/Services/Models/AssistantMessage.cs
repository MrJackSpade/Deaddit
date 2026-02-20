namespace Deaddit.Services.Models
{
    public class AssistantMessage : Message
    {
        public AssistantMessage(string content)
        {
            Content = content;
        }

        public override string Role => "assistant";
    }
}