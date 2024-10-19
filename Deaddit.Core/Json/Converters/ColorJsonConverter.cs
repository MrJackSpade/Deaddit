using Deaddit.Core.Utils.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deaddit.Core.Json.Converters
{
    public class ColorJsonConverter : JsonConverter<DynamicColor>
    {
        public override DynamicColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                _ = reader.Read();
                return DynamicColor.Parse("#000000");
            }

            string? hex = reader.GetString();
            return DynamicColor.Parse(hex);
        }

        public override void Write(Utf8JsonWriter writer, DynamicColor value, JsonSerializerOptions options)
        {
            string hex = value.ToArgbHex();
            writer.WriteStringValue(hex);
        }
    }
}