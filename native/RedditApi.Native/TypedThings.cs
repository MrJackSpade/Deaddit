using Reddit.Api.Models.Enums;
using Reddit.Api.Models.Json.Common;
using Reddit.Api.Models.Json.Listings;
using Reddit.Api.Models.Json.Messages;
using System.Text.Json;

namespace RedditApi.Native
{
    /// <summary>
    /// Passes things that Reddit.Api returns as raw JSON through Reddit.Api's model for their kind,
    /// as the C# app did, so the host gets every thing in the same shape whichever call returned it.
    /// The models' converters are what decode Reddit's HTML escaping (body, selftext, flair text and
    /// their _html forms), so a thing that skips them reaches the host still escaped.
    /// </summary>
    public static class TypedThings
    {
        /// <summary>
        /// A listing of mixed things (feeds, profiles, saved, inbox) with each child's data read
        /// into its model, like the C# app's RedditModelMapper.MapThings. Other kinds throw, as there.
        /// </summary>
        public static Listing<Thing<object>>? Listing(Listing<Thing<object>>? listing, JsonSerializerOptions options)
        {
            if (listing?.Data?.Children == null)
            {
                return listing;
            }

            foreach (Thing<object>? child in listing.Data.Children)
            {
                if (child?.Data is not JsonElement data)
                {
                    throw new InvalidOperationException($"A thing in the listing ({child?.Kind.ToString() ?? "null"}) has no data.");
                }

                child.Data = child.Kind switch
                {
                    ThingKind.Link => Read<Link>(data, options),
                    ThingKind.Comment => WithTypedReplies(Read<Comment>(data, options), options),
                    ThingKind.Message => Read<Message>(data, options),
                    _ => throw new NotSupportedException($"Thing kind '{child.Kind}' is not supported in a listing.")
                };
            }

            return listing;
        }

        /// <summary>
        /// A comment listing whose comments' replies, which Reddit.Api keeps as raw JSON, are read
        /// into comment listings too, all the way down, like the C# app's MapCommentReplies.
        /// </summary>
        public static Listing<Thing<Comment>>? WithTypedReplies(Listing<Thing<Comment>>? listing, JsonSerializerOptions options)
        {
            foreach (Thing<Comment> child in listing?.Data?.Children ?? [])
            {
                if (child.Data != null)
                {
                    WithTypedReplies(child.Data, options);
                }
            }

            return listing;
        }

        /// <summary>
        /// Reddit sends replies as "" when there are none, or as a listing of comments.
        /// </summary>
        private static Comment WithTypedReplies(Comment comment, JsonSerializerOptions options)
        {
            if (comment.Replies is JsonElement { ValueKind: JsonValueKind.Object } replies)
            {
                comment.Replies = WithTypedReplies(Read<Listing<Thing<Comment>>>(replies, options), options);
            }

            return comment;
        }

        private static T Read<T>(JsonElement data, JsonSerializerOptions options)
        {
            return data.Deserialize<T>(options) ?? throw new InvalidOperationException($"A {typeof(T).Name} in the listing is null.");
        }
    }
}
