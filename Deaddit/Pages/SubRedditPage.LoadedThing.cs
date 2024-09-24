using Deaddit.Core.Reddit.Models.Api;
using Maui.WebComponents.Interfaces;

namespace Deaddit.Pages
{
    public partial class SubRedditPage
    {
        private class LoadedThing
        {
            public ApiThing Post { get; set; }

            public IWebComponent PostComponent { get; set; }
        }
    }
}