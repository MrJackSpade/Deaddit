using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Components;

namespace Deaddit.Pages
{
    public partial class SubRedditPage
    {
        private class LoadedThing
        {
            public ApiThing Post { get; set; }

            public WebComponent PostComponent { get; set; }
        }
    }
}