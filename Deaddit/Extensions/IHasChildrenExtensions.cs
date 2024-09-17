using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Interfaces;
using Deaddit.MAUI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                ContentView? childComponent = null;

                if (renderChild is ApiComment comment)
                {
                    RedditCommentComponent commentComponent = target.AppNavigator.CreateCommentComponent(comment, target.Post, target.SelectionGroup);
                    commentComponent.AddChildren(comment.GetReplies());
                    commentComponent.OnDelete += (s, e) => target.ChildContainer.Remove(commentComponent);
                    childComponent = commentComponent;
                }
                else if (child is ApiMore more)
                {
                    MoreCommentsComponent mcomponent = target.AppNavigator.CreateMoreCommentsComponent(more);
                    mcomponent.OnClick += target.MoreCommentsClick;
                    childComponent = mcomponent;
                } else if(child is ApiMessage message)
                {
                    RedditMessageComponent mcomponent = target.AppNavigator.CreateMessageComponent(message);
                    childComponent = mcomponent;
                }

                if (childComponent is null)
                {
                    throw new NotImplementedException();
                }

                target.InitChildContainer();

                target.ChildContainer.Add(childComponent);
            }

            foreach (KeyValuePair<CollapsedReasonKind, List<ApiThing>> collapsedComments in toRender)
            {
                if (collapsedComments.Key == CollapsedReasonKind.None)
                {
                    continue;
                }

                IMore more = new CollapsedMore(collapsedComments.Value.OfType<ApiComment>());

                MoreCommentsComponent mcomponent = target.AppNavigator.CreateMoreCommentsComponent(more);

                mcomponent.OnClick += target.MoreCommentsClick;

                target.InitChildContainer();

                target.ChildContainer.Add(mcomponent);
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