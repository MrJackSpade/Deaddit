using Deaddit.Core.Reddit.Models;

namespace Deaddit.Core.Reddit.Interfaces
{
    /// <summary>
    /// Interface for "more comments" placeholder items.
    /// </summary>
    public interface IMore
    {
        /// <summary>
        /// List of child comment IDs to load.
        /// </summary>
        List<string> ChildNames { get; }

        /// <summary>
        /// Count of additional comments.
        /// </summary>
        int? Count { get; }

        /// <summary>
        /// Parent thing containing this more item.
        /// </summary>
        ApiThing? Parent { get; }
    }
}
