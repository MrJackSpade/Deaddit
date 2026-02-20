using Maui.WebComponents.Components;
using Reddit.Api.Models.Api;

namespace Deaddit.Pages
{
    public partial class ThingCollectionPage
    {
        private class LoadedThing
        {
            public ApiThing Post { get; set; }

            public WebComponent PostComponent { get; set; }
        }
    }
}