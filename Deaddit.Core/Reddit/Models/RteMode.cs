using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public enum RteMode
    {
        [EnumMember(Value = null)]
        Undefined,

        [EnumMember(Value = "markdown")]
        Markdown
    }
}
