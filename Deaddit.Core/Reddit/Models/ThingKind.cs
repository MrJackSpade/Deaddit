using System.Runtime.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public enum ThingKind
    {
        [EnumMember(Value = "t1")]
        Comment,

        [EnumMember(Value = "t2")]
        Account,

        [EnumMember(Value = "t3")]
        Link,

        [EnumMember(Value = "t4")]
        Message,

        [EnumMember(Value = "t5")]
        Subreddit,

        [EnumMember(Value = "t6")]
        Award,

        [EnumMember(Value = "more")]
        More,

        [EnumMember(Value = "Listing")]
        Listing,

        [EnumMember(Value = "LabeledMulti")]
        Multi
    }
}