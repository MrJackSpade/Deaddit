using System.Runtime.Serialization;

namespace Deaddit.Core.Configurations.Models
{
    public enum PostState
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "Block")]
        Block = 1,

        [EnumMember(Value = "Mark As Read")]
        Visited = 2,
    }
}