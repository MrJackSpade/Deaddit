﻿using Reddit.Api.Models.Api;
using Maui.WebComponents.Components;

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