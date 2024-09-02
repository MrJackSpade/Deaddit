using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Models.Json
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
        More
    }
}