﻿using Deaddit.Components;
using Deaddit.Core.Reddit.Models.ThingDefinitions;

namespace Deaddit.EventArguments
{
    public class SubRedditSubscriptionRemoveEventArgs(SubscriptionComponent component, ThingDefinition thing) : EventArgs
    {
        public SubscriptionComponent Component { get; private set; } = component;

        public ThingDefinition Thing { get; private set; } = thing;
    }
}