﻿using System.Runtime.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public enum DistinguishedKind
    {
        [EnumMember(Value = null)]
        None,

        [EnumMember(Value = "moderator")]
        Moderator
    }
}
