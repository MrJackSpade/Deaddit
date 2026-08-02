using Deaddit.Configurations.Ai;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deaddit.Json.Converters
{
    /// <summary>
    /// Serializes <see cref="ClaudeModel"/> by member name, mirroring <see cref="JsonStringEnumConverter"/>.
    /// Unlike that converter, a stored value that no longer maps to a member - for example a model that has
    /// since been removed from the list - resolves to <see cref="ClaudeModel.None"/> instead of throwing, so
    /// an outdated saved configuration loads with a blank model rather than crashing the app.
    /// </summary>
    public class ClaudeModelJsonConverter : JsonConverter<ClaudeModel>
    {
        public override ClaudeModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? name = reader.GetString();

                if (Enum.TryParse(name, ignoreCase: true, out ClaudeModel model) && Enum.IsDefined(model))
                {
                    return model;
                }
            }

            return ClaudeModel.None;
        }

        public override void Write(Utf8JsonWriter writer, ClaudeModel value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
