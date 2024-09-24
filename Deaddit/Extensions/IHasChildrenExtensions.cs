using Deaddit.Components.WebComponents;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components;
using Maui.WebComponents.Components;

namespace Deaddit.Extensions
{
    internal static class IHasChildrenExtensions
    {
        public static void AddChildren(this IHasChildren target, IEnumerable<ApiThing> children, bool loadMore = false)
        {
            Dictionary<CollapsedReasonKind, List<ApiThing>> toRender;

            if (!loadMore)
            {
                toRender = children.GroupByCollasedReason();
            }
            else
            {
                toRender = new Dictionary<CollapsedReasonKind, List<ApiThing>>
                {
                    { CollapsedReasonKind.None, children.ToList() }
                };
            }

            foreach (ApiThing? child in toRender[CollapsedReasonKind.None])
            {
                ApiThing renderChild = child;

                if (!ShouldRender(target, child))
                {
                    continue;
                }

                WebComponent? childComponent = null;

                if (renderChild is ApiComment comment)
                {
                    RedditCommentWebComponent commentComponent = target.AppNavigator.CreateCommentWebComponent(comment, target.Post, target.SelectionGroup);
                    commentComponent.AddChildren(comment.GetReplies());
                    commentComponent.OnDelete += (s, e) => target.ChildContainer.Children.Remove(commentComponent);
                    childComponent = commentComponent;
                }
                else if (child is ApiMore more)
                {
                    MoreCommentsWebComponent mcomponent = target.AppNavigator.CreateMoreCommentsWebComponent(more);
                    mcomponent.OnClick += (s, e) => target.MoreCommentsClick(mcomponent, mcomponent);
                    childComponent = mcomponent;
                }
                else if (child is ApiMessage message)
                {
                    RedditMessageWebComponent mcomponent = target.AppNavigator.CreateMessageWebComponent(message);
                    childComponent = mcomponent;
                }

                if (childComponent is null)
                {
                    throw new NotImplementedException();
                }

                target.ChildContainer.Children.Add(childComponent);
            }

            foreach (KeyValuePair<CollapsedReasonKind, List<ApiThing>> collapsedComments in toRender)
            {
                if (collapsedComments.Key == CollapsedReasonKind.None)
                {
                    continue;
                }

                IMore more = new CollapsedMore(collapsedComments.Value.OfType<ApiComment>());

                MoreCommentsWebComponent mcomponent = target.AppNavigator.CreateMoreCommentsWebComponent(more);

                mcomponent.OnClick += (s,e) => target.MoreCommentsClick(mcomponent, mcomponent);

                target.ChildContainer.Children.Add(mcomponent);
            }
        }

        private static bool ShouldRender(IHasChildren target, ApiThing thing)
        {
            if (!target.BlockConfiguration.IsAllowed(thing))
            {
                return false;
            }

            if (thing.Id == target.Post.Id)
            {
                return false;
            }

            if (thing.IsRemoved())
            {
                return false;
            }

            if (thing.IsDeleted() && thing is ApiComment comment && !comment.HasChildren())
            {
                return false;
            }

            return true;
        }
    }
}