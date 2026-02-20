using Deaddit.Configurations.Ai;
using System.ComponentModel;
using System.Reflection;

namespace Deaddit.Extensions
{
    public static class ClaudeModelExtensions
    {
        public static string ToModelId(this ClaudeModel model)
        {
            FieldInfo? field = model.GetType().GetField(model.ToString());
            DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? model.ToString();
        }
    }
}
