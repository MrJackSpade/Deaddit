using Deaddit.Core.Reddit.Interfaces;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiMore : ApiThing, IMore
    {
        public List<string> ChildNames { get; init; } = [];

        public int? Count { get; init; }

        public int? Depth { get; init; }
    }
}