using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Core.Reddit.Models.Api
{
    public enum RteMode
    {
        [EnumMember(Value = null)]
        Undefined,

        [EnumMember(Value = "markdown")]
        Markdown
    }
}
