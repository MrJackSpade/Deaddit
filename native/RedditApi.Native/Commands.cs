using Reddit.Api.Client;
using Reddit.Api.Models;
using Reddit.Api.Models.Enums;
using Reddit.Api.Models.Json.Common;
using Reddit.Api.Models.Json.LinksComments;
using Reddit.Api.Models.Json.Listings;
using Reddit.Api.Models.Json.Media;
using Reddit.Api.Models.Json.Multis;
using Reddit.Api.Models.Json.Search;
using System.Text.Json;

namespace RedditApi.Native
{
    /// <summary>
    /// Maps each command name to a Reddit.Api call. Argument names are the C# parameter names;
    /// request objects use Reddit.Api's own JSON shape (snake_case).
    /// </summary>
    internal static class Commands
    {
        public static async Task<object?> RunAsync(RedditClient client, string method, JsonElement args)
        {
            switch (method)
            {
                // ----- Session -----
                case "SetBearerToken":
                    client.SetBearerToken(Model<BearerToken>(args, "token", BridgeJson.Default.Options));
                    return null;
                case "GetSession":
                    return new Dictionary<string, object?> { ["isAuthenticated"] = client.IsAuthenticated, ["authenticatedUser"] = client.AuthenticatedUser };
                case "ValidateBearerToken":
                    return await client.ValidateBearerToken(Model<BearerToken>(args, "token", BridgeJson.Default.Options));
                case "GetTokenOwner":
                    return await client.GetTokenOwner(Str(args, "accessToken"), OptStr(args, "sessionCookies"));
                case "EnsureAuthenticated":
                    await client.EnsureAuthenticatedAsync();
                    return null;
                case "TryAuthenticate":
                    await client.TryAuthenticateAsync();
                    return null;

                // ----- Reads -----
                case "GetListing":
                    return await client.GetListingAsync<object>(Str(args, "endpoint"), OptModel<ListingParameters>(args, "parameters"));
                case "GetSortedPosts":
                    return await client.GetSortedPostsAsync(Str(args, "path"), OptModel<ListingParameters>(args, "parameters"));
                case "GetComments":
                    {
                        (Thing<Link>? post, Listing<Thing<Comment>>? comments) = await client.GetCommentsAsync(
                            Str(args, "articleId"), OptStr(args, "commentId"), OptStr(args, "sort"), OptInt(args, "limit"), OptInt(args, "depth"), OptInt(args, "context"));
                        return new Dictionary<string, object?> { ["post"] = post, ["comments"] = comments };
                    }
                case "GetMoreChildren":
                    return await client.GetMoreChildrenAsync(Str(args, "linkFullname"), StrList(args, "children"), OptStr(args, "sort"), OptInt(args, "depth"), OptStr(args, "subreddit"));
                case "GetPost":
                    return await client.GetPostAsync(Str(args, "idOrFullname"));
                case "GetById":
                    return await client.GetByIdAsync(StrList(args, "fullnames"));
                case "Search":
                    return await client.SearchAsync(Model<SearchParameters>(args, "parameters"));
                case "GetSubredditAbout":
                    return await client.GetSubredditAboutAsync(Str(args, "subreddit"));
                case "GetSubredditRules":
                    return await client.GetSubredditRulesAsync(Str(args, "subreddit"));
                case "GetUserAbout":
                    return await client.GetUserAboutAsync(Str(args, "username"));
                case "GetUserDataByIds":
                    return await client.GetUserDataByIdsAsync(StrList(args, "accountIds"));
                case "GetMyMultis":
                    return await client.GetMyMultisAsync();

                // ----- Writes -----
                case "CommentRichText":
                    return await client.CommentAsync(Str(args, "parentFullname"), Model<RichTextDocument>(args, "richtext"));
                case "CommentMarkdown":
                    return await client.CommentAsync(Str(args, "parentFullname"), Str(args, "markdown"));
                case "EditRichText":
                    return await client.EditAsync(Str(args, "fullname"), Model<RichTextDocument>(args, "richtext"));
                case "EditMarkdown":
                    return await client.EditAsync(Str(args, "fullname"), Str(args, "markdown"));
                case "Submit":
                    return await client.SubmitAsync(Model<SubmitRequest>(args, "request"));
                case "DeleteThing":
                    return await client.DeleteThingAsync(Str(args, "fullname"));
                case "Vote":
                    return await client.VoteAsync(Str(args, "fullname"), args.GetProperty("direction").GetInt32());
                case "Save":
                    return await client.SaveAsync(Str(args, "fullname"));
                case "Unsave":
                    return await client.UnsaveAsync(Str(args, "fullname"));
                case "Hide":
                    return await client.HideAsync(Str(args, "fullname"));
                case "Unhide":
                    return await client.UnhideAsync(Str(args, "fullname"));
                case "Lock":
                    return await client.LockAsync(Str(args, "fullname"));
                case "Unlock":
                    return await client.UnlockAsync(Str(args, "fullname"));
                case "SetSendReplies":
                    return await client.SetSendRepliesAsync(Str(args, "fullname"), args.GetProperty("state").GetBoolean());
                case "Distinguish":
                    return await client.DistinguishAsync(Str(args, "fullname"), Enum.Parse<DistinguishHow>(Str(args, "how"), ignoreCase: true), OptBool(args, "sticky"));
                case "Report":
                    return await client.ReportAsync(Str(args, "fullname"), OptStr(args, "reason"), OptStr(args, "siteReason"), OptStr(args, "ruleReason"));
                case "Subscribe":
                    return await client.SubscribeAsync(Str(args, "subreddit"), args.GetProperty("subscribe").GetBoolean());
                case "ReadMessage":
                    return await client.ReadMessageAsync(Str(args, "fullname"));
                case "UnreadMessage":
                    return await client.UnreadMessageAsync(Str(args, "fullname"));
                case "ComposeMessage":
                    return await client.ComposeMessageAsync(Str(args, "to"), Str(args, "subject"), Str(args, "body"));
                case "CreateOrUpdateMulti":
                    return await client.CreateOrUpdateMultiAsync(Str(args, "multipath"), Model<MultiCreateRequest>(args, "request"));
                case "DeleteMulti":
                    return await client.DeleteMultiAsync(Str(args, "multipath"));
                case "AddSubredditToMulti":
                    return await client.AddSubredditToMultiAsync(Str(args, "multipath"), Str(args, "subreddit"));
                case "RemoveSubredditFromMulti":
                    return await client.RemoveSubredditFromMultiAsync(Str(args, "multipath"), Str(args, "subreddit"));

                // ----- Media -----
                case "GetMediaAssetUploadLease":
                    return await client.GetMediaAssetUploadLeaseAsync(Str(args, "filepath"), Str(args, "mimetype"));
                case "UploadMediaToS3":
                    {
                        using MemoryStream stream = new(Convert.FromBase64String(Str(args, "fileBase64")));
                        return await client.UploadMediaToS3Async(Model<MediaAssetResponse>(args, "lease"), stream, Str(args, "filename"), Str(args, "mimetype"));
                    }

                default:
                    throw new ArgumentException($"Unknown command '{method}'.");
            }
        }

        private static string Str(JsonElement args, string name)
        {
            return args.GetProperty(name).GetString() ?? throw new ArgumentException($"Argument '{name}' is null.");
        }

        private static string? OptStr(JsonElement args, string name)
        {
            return args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out JsonElement v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null;
        }

        private static int? OptInt(JsonElement args, string name)
        {
            return args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out JsonElement v) && v.ValueKind != JsonValueKind.Null ? v.GetInt32() : null;
        }

        private static bool? OptBool(JsonElement args, string name)
        {
            return args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out JsonElement v) && v.ValueKind != JsonValueKind.Null ? v.GetBoolean() : null;
        }

        private static List<string> StrList(JsonElement args, string name)
        {
            return args.GetProperty(name).EnumerateArray().Select(e => e.GetString()!).ToList();
        }

        private static T Model<T>(JsonElement args, string name, JsonSerializerOptions? options = null)
        {
            return args.GetProperty(name).Deserialize<T>(options ?? Bridge.ModelJson)
                ?? throw new ArgumentException($"Argument '{name}' is null.");
        }

        private static T? OptModel<T>(JsonElement args, string name) where T : class
        {
            return args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out JsonElement v) && v.ValueKind != JsonValueKind.Null
                ? v.Deserialize<T>(Bridge.ModelJson)
                : null;
        }
    }
}
