using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public enum ApiPostSort
    {
        [EnumMember(Value = null)]
        Undefined = 0,

        [EnumMember(Value = "hot")]
        Hot = 1,

        [EnumMember(Value = "new")]
        New = 2,

        [EnumMember(Value = "rising")]
        Rising = 3,

        [EnumMember(Value = "top")]
        Top = 4
    }
}