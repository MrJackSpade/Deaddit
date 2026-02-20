namespace Deaddit.Services.Models
{
    public class UserMessage : Message
    {
        public UserMessage(string content)
        {
            Content = content;
        }

        public override string Role => "user";
    }
}