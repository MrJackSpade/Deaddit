using Deaddit.Core.Reddit.Models.Api;
using Reddit.Api.Models.Enums;

namespace Deaddit.Extensions
{
    internal static class IEnumerableApiThingExtensions
    {
        public static Dictionary<CollapsedReasonCode, List<ApiThing>> GroupByCollasedReason(this IEnumerable<ApiThing> children)
        {
            Dictionary<CollapsedReasonCode, List<ApiThing>> toRender = [];

            toRender.Add(CollapsedReasonCode.Null, []);

            foreach (ApiThing thing in children)
            {
                CollapsedReasonCode collapsedReasonCode = CollapsedReasonCode.Null;

                if (thing is ApiComment comment)
                {
                    collapsedReasonCode = comment.CollapsedReasonCode;
                }

                if (!toRender.ContainsKey(collapsedReasonCode))
                {
                    toRender.Add(collapsedReasonCode, []);
                }

                toRender[collapsedReasonCode].Add(thing);
            }

            return toRender;
        }
    }
}