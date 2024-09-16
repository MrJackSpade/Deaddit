using Deaddit.Core.Reddit.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Extensions
{
    internal static class IEnumerableApiThingExtensions
    {
        public static Dictionary<CollapsedReasonKind, List<ApiThing>> GroupByCollasedReason(this IEnumerable<ApiThing> children)
        {
            Dictionary<CollapsedReasonKind, List<ApiThing>> toRender = [];

            toRender.Add(CollapsedReasonKind.None, []);

            foreach (ApiThing thing in children)
            {
                CollapsedReasonKind collapsedReasonKind = CollapsedReasonKind.None;

                if (thing is ApiComment comment)
                {
                    collapsedReasonKind = comment.CollapsedReasonCode;
                }

                if (!toRender.ContainsKey(collapsedReasonKind))
                {
                    toRender.Add(collapsedReasonKind, []);
                }

                toRender[collapsedReasonKind].Add(thing);
            }

            return toRender;
        }
    }
}
