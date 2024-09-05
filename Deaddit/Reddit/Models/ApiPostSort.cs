using System.Runtime.Serialization;

namespace Deaddit.Reddit.Models
{
    public enum ApiPostSort
    {
        Undefined, 

        [EnumMember(Value = "hot")]
        Hot,

        [EnumMember(Value = "new")]
        New,

        [EnumMember(Value = "rising")]
        Rising,

        [EnumMember(Value = "top")]
        Top,

        [EnumMember(Value = "controversial")]
        Controversial
    }
}