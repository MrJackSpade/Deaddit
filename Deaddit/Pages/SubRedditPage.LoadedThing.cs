using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Pages
{
    public partial class SubRedditPage
    {
        private class LoadedThing
        {
            public ApiThing Post { get; set; }

            public VisualElement PostComponent { get; set; }
        }
    }
}