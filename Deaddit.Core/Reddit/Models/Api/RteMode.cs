using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public enum RteMode
    {
        [EnumMember(Value = null)]
        Undefined,

        [EnumMember(Value = "markdown")]
        Markdown
    }
}
