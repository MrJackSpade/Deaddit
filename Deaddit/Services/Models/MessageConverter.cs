using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deaddit.Services.Models
{
    /// <summary>
    /// Custom JSON converter for Message polymorphism
    /// </summary>
    public class MessageConverter : JsonConverter<Message>
    {
        public override Message Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;
            string? role = root.GetProperty("role").GetString();
            string? content = root.GetProperty("content").GetString();

            return role switch
            {
                "user" => new UserMessage(content),
                "assistant" => new AssistantMessage(content),
                _ => throw new JsonException($"Unknown role: {role}")
            };
        }

        public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("role", value.Role);
            writer.WriteString("content", value.Content);
            writer.WriteEndObject();
        }
    }
}