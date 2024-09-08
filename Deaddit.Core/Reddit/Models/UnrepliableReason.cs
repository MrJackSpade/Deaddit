using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Core.Reddit.Models
{
    public enum UnrepliableReason
    {
        [EnumMember(Value = null)]
        None = 0,

        [EnumMember(Value = "NEAR_BLOCKER")]
        NearBlocker = 1
    }
}
