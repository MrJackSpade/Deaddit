using System.Text.Json;
using System.Text.Json.Serialization;
using Color = Microsoft.Maui.Graphics.Color;

namespace Deaddit.Converters
{
    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType != JsonTokenType.String)
            {
                object value = reader.Read();
                return Color.Parse("#000000");
            }

            string hex = reader.GetString();
            return Color.FromRgba(hex);
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            string hex = value.ToRgbaHex();
            writer.WriteStringValue(hex);
        }
    }
}