using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public enum ApiCommentSort
    {
        [EnumMember(Value = null)]
        Undefined = 0,

        [EnumMember(Value = "new")]
        New = 1,

        [EnumMember(Value = "top")]
        Top = 2,

        [EnumMember(Value = "controversial")]
        Controversial = 3,

        [EnumMember(Value = "confidence")]
        Confidence = 4,

        [EnumMember(Value = "qa")]
        Qa = 5,

        [EnumMember(Value = "old")]
        Old = 6,

        [EnumMember(Value = "random")]
        Random = 7,

        [EnumMember(Value = "live")]
        Live = 8,
    }
}