using System.Text;

namespace Maui.WebComponents.Classes
{
    public class StyleRule
    {
        public string CssSelector { get; set; }

        public Dictionary<string, string> Styles { get; init; } = [];

        public override string ToString()
        {
            StringBuilder stringBuilder = new();

            stringBuilder.Append($"{CssSelector} {{");

            foreach (KeyValuePair<string, string> style in Styles)
            {
                stringBuilder.Append($"{style.Key}: {style.Value};");
            }

            stringBuilder.Append('}');

            return stringBuilder.ToString();
        }
    }
}