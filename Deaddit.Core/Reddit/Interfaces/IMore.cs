using Deaddit.Core.Reddit.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Deaddit.Core.Reddit.Interfaces
{
    public interface IMore
    {
        List<string> ChildNames { get; }

        int? Count { get; }

        ApiThing Parent { get; }
    }
}