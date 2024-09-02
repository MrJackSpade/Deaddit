using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Models.Json.Response
{
    public enum DistinguishedKind
    {
        [EnumMember(Value = null)]
        None,

        [EnumMember(Value = "moderator")]
        Moderator
    }
}
