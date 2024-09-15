using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Interfaces
{
    public interface IMore
    {
        List<string> ChildNames { get; }

        int? Count { get; }

        ApiThing Parent { get; }
    }
}