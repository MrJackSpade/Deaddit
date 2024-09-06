using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Reddit.Models
{
    public enum RemovalReason
    {
        [EnumMember(Value = null)]
        None,

        [EnumMember(Value = "legal")]
        Legal
    }
}
