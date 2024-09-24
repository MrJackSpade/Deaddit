using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Attributes;

namespace Maui.WebComponents.Components
{
    [HtmlEntity("reddit-more-comments")]
    public class MoreCommentsWebComponent : DivComponent, IMore
    {
        public List<string> ChildNames { get; }
        public int? Count { get; }
        public ApiThing Parent { get; }
    }
}