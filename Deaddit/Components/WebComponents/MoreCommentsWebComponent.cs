using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Attributes;

namespace Maui.WebComponents.Components
{
    [HtmlEntity("reddit-more-comments")]
    public class MoreCommentsWebComponent : DivComponent, IMore
    {
        private readonly IMore _more;

        private readonly ApplicationStyling _applicationStyling;

        public List<string> ChildNames { get; } = [];

        public int? Count { get; }

        public ApiThing Parent { get; }

        public MoreCommentsWebComponent(IMore more, ApplicationStyling applicationStyling)
        {
            this._more = more;
            this._applicationStyling = applicationStyling;
        }
    }
}