using Deaddit.Core.Reddit.Mapping;
using Reddit.Api.Extensions;
using Reddit.Api.Interfaces;
using Reddit.Api.Models;
using Reddit.Api.Models.Api;
using Reddit.Api.Models.Json.Common;
using Reddit.Api.Models.Json.Listings;
using Reddit.Api.Models.Requests;
using Reddit.Api.Models.ThingDefinitions;
using System.Diagnostics;
using NewClient = Reddit.Api.Client;

namespace Deaddit.Core.Reddit
{
    /// <summary>
    /// Reddit service that wraps the new Reddit API client and provides
    /// business logic layer functionality using Api models.
    /// </summary>
    public class RedditService : IRedditClient
    {
        private readonly NewClient.IRedditClient _client;
        private readonly HttpClient _httpClient;

        public RedditService(IRedditCredentials credentials, HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // Create the new client internally using provided credentials
            var clientCredentials = new NewClient.RedditCredentials
            {
                Username = credentials.UserName,
                Password = credentials.Password,
                AppKey = credentials.AppKey,
                AppSecret = credentials.AppSecret
            };

            _client = new NewClient.RedditClient(clientCredentials, httpClient);
        }

        public bool CanLogIn => _client.CanAuthenticate;

        public bool IsLoggedIn => _client.IsAuthenticated;

        public string? LoggedInUser => _client.AuthenticatedUser;

        public async Task<ApiSubReddit?> About(SubRedditDefinition subreddit)
        {
            try
            {
                await TryAuthenticate();

                Stopwatch stopwatch = Stopwatch.StartNew();

                var result = await _client.GetSubredditAboutAsync(subreddit.Name);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in About method: {stopwatch.ElapsedMilliseconds}ms");

                return RedditModelMapper.Map(result);
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
                return null;
            }
        }

        public async Task<ApiComment?> Comment(ApiThing replyTo, string comment)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                var result = await _client.CommentAsync(replyTo.Name, comment);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Comment method: {stopwatch.ElapsedMilliseconds}ms");

                return RedditModelMapper.Map(result);
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
                return null;
            }
        }

        public async Task<List<ApiThing>> Comments(ApiPost post, ApiComment? focusComment)
        {
            List<ApiThing> toReturn = [];

            try
            {
                await TryAuthenticate();

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (_, comments) = await _client.GetCommentsAsync(
                    post.Id!,
                    focusComment?.Id,
                    sort: null,
                    limit: null,
                    depth: null);

                ApiThing responseParent = (ApiThing?)focusComment ?? post;

                if (comments?.Data?.Children != null)
                {
                    foreach (var child in comments.Data.Children)
                    {
                        if (child?.Data == null)
                        {
                            continue;
                        }

                        // Check if this is a "more" item
                        if (child.Kind == "more")
                        {
                            var more = RedditModelMapper.MapMore(child.Data);
                            more.Parent = responseParent;
                            toReturn.Add(more);
                        }
                        else
                        {
                            var apiComment = RedditModelMapper.Map(child.Data);
                            if (apiComment != null && apiComment.Id != post.Id)
                            {
                                apiComment.Parent = responseParent;
                                MapCommentReplies(apiComment, child.Data, apiComment);
                                toReturn.Add(apiComment);
                            }
                        }
                    }
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Comments method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }

            return toReturn;
        }

        public async Task<ApiPost?> CreatePost(string subreddit, string title, string content, PostKind kind)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                string kindString = kind.ToString().ToLower();
                var request = new global::Reddit.Api.Models.Json.LinksComments.SubmitRequest
                {
                    Subreddit = subreddit,
                    Title = title,
                    Kind = kindString
                };

                if (kind is PostKind.Self)
                {
                    request.Text = content;
                }
                else if (kind == PostKind.Link)
                {
                    request.Url = content;
                }

                var result = await _client.SubmitAsync(request);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in CreatePost method: {stopwatch.ElapsedMilliseconds}ms");

                if (result?.Name != null)
                {
                    return await GetPost(result.Name);
                }

                return null;
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
                return null;
            }
        }

        public async Task Delete(ApiThing thing)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                await _client.DeleteThingAsync(thing.Name);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Delete method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task<Dictionary<string, UserPartial>> GetPartialUserData(IEnumerable<string> usernames)
        {
            try
            {
                var usernameList = usernames.ToList();
                if (usernameList.Count == 0)
                {
                    return [];
                }

                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                var result = await _client.GetUserDataByIdsAsync(usernameList);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in GetPartialUserData method: {stopwatch.ElapsedMilliseconds}ms");

                if (result == null || result.Count == 0)
                {
                    return [];
                }

                var toReturn = new Dictionary<string, UserPartial>();
                foreach (var kvp in result)
                {
                    toReturn[kvp.Key] = new UserPartial
                    {
                        Name = kvp.Value.Name,
                        ProfileColor = kvp.Value.ProfileColor,
                        ProfileImg = kvp.Value.ProfileImg,
                        ProfileOver18 = kvp.Value.ProfileOver18 ?? false
                    };
                }

                return toReturn;
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
                return [];
            }
        }

        public async Task<ApiPost?> GetPost(string id)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                string fullname = id.StartsWith("t3_") ? id : $"t3_{id}";

                var result = await _client.GetInfoAsync([fullname]);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in GetPost method: {stopwatch.ElapsedMilliseconds}ms");

                if (result?.Data?.Children?.Count > 0)
                {
                    return RedditModelMapper.Map(result.Data.Children[0]);
                }

                return null;
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
                return null;
            }
        }

        public async Task<List<ApiPost>> GetPosts(IEnumerable<string> ids)
        {
            try
            {
                await EnsureAuthenticated();

                var fullnames = ids.Select(id => id.StartsWith("t3_") ? id : $"t3_{id}");

                var result = await _client.GetByIdAsync(fullnames);

                return RedditModelMapper.MapPostsTyped(result);
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
                return [];
            }
        }

        public async Task<List<ApiThing>> GetPosts<T>(ApiEndpointDefinition endpointDefinition, T sort, int pageSize, string? after = null, Region region = Region.GLOBAL) where T : Enum
        {
            List<ApiThing> toReturn = [];

            try
            {
                await TryAuthenticate();

                Stopwatch stopwatch = Stopwatch.StartNew();

                // Parse the endpoint to determine the subreddit and sort type
                string endpoint = endpointDefinition.Url.TrimStart('/');
                string? subreddit = null;

                if (endpoint.StartsWith("r/"))
                {
                    var parts = endpoint.Split('/');
                    if (parts.Length >= 2)
                    {
                        subreddit = parts[1];
                    }
                }

                var parameters = new ListingParameters
                {
                    After = after,
                    Limit = Math.Min(100, pageSize),
                    Geo = region != Region.GLOBAL ? region.ToString() : null
                };

                Listing<Thing<Link>>? result = null;
                string sortName = sort.ToString().ToLower();

                // Route to appropriate method based on sort
                result = sortName switch
                {
                    "hot" => await _client.GetHotAsync(subreddit, parameters),
                    "new" => await _client.GetNewAsync(subreddit, parameters),
                    "top" => await _client.GetTopAsync(subreddit, parameters),
                    "controversial" => await _client.GetControversialAsync(subreddit, parameters),
                    "rising" => await _client.GetRisingAsync(subreddit, parameters),
                    "best" => await _client.GetBestAsync(parameters),
                    _ => await _client.GetHotAsync(subreddit, parameters)
                };

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in GetPosts method: {stopwatch.ElapsedMilliseconds}ms");

                if (result?.Data?.Children == null)
                {
                    return toReturn;
                }

                foreach (var child in result.Data.Children)
                {
                    if (child?.Data != null)
                    {
                        var post = RedditModelMapper.Map(child.Data);
                        if (post != null)
                        {
                            toReturn.Add(post);
                        }
                    }

                    if (toReturn.Count >= pageSize)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }

            return toReturn;
        }

        public async Task<Stream> GetStream(string url)
        {
            await EnsureAuthenticated();
            return await _httpClient.GetStreamAsync(url);
        }

        public async Task<ApiUser?> GetUserData(string username)
        {
            try
            {
                await TryAuthenticate();

                var result = await _client.GetUserAboutAsync(username);
                return RedditModelMapper.Map(result);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task MarkRead(ApiThing message, bool state)
        {
            try
            {
                await EnsureAuthenticated();

                if (state)
                {
                    await _client.ReadMessageAsync(message.Name);
                }
                else
                {
                    await _client.UnreadMessageAsync(message.Name);
                }
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task Message(ApiUser user, string subject, string body)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                await _client.ComposeMessageAsync(user.Name!, subject, body);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Message method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task<List<ApiThing>> MoreComments(ApiPost post, IMore moreItem)
        {
            List<ApiThing> toReturn = [];

            try
            {
                await EnsureAuthenticated();

                if (moreItem.ChildNames == null || moreItem.ChildNames.Count == 0)
                {
                    return toReturn;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();

                var result = await _client.GetMoreChildrenAsync(
                    post.Name,
                    moreItem.ChildNames,
                    sort: null,
                    depth: 999);

                if (result != null)
                {
                    // Build tree structure
                    Dictionary<string, ApiComment> tree = [];

                    foreach (var thing in result)
                    {
                        if (thing?.Data != null && thing.Kind != "more")
                        {
                            var comment = RedditModelMapper.Map(thing.Data);
                            if (comment != null)
                            {
                                tree[comment.Name] = comment;
                            }
                        }
                    }

                    // Link parents
                    List<ApiThing> allThings = [];
                    foreach (var thing in result)
                    {
                        if (thing?.Data == null)
                        {
                            continue;
                        }

                        if (thing.Kind == "more")
                        {
                            var more = RedditModelMapper.MapMore(thing.Data);
                            allThings.Add(more);
                        }
                        else if (tree.TryGetValue(thing.Data.Name, out var comment))
                        {
                            allThings.Add(comment);
                        }
                    }

                    // Nest children under parents
                    foreach (var apiThing in allThings.ToList())
                    {
                        if (apiThing is ApiComment apiComment)
                        {
                            if (apiComment.ParentId != null && tree.TryGetValue(apiComment.ParentId, out var parent))
                            {
                                parent.AddReply(apiComment);
                                allThings.Remove(apiThing);
                            }
                        }

                        if (apiThing is ApiMore apiMore)
                        {
                            if (apiMore.ParentId != null && tree.TryGetValue(apiMore.ParentId, out var parent))
                            {
                                parent.AddReply(apiMore);
                                allThings.Remove(apiThing);
                            }
                        }
                    }

                    // Set parent for remaining top-level items
                    foreach (var apiThing in allThings)
                    {
                        if (moreItem.Parent != null)
                        {
                            SetParent(moreItem.Parent, apiThing);
                        }
                        toReturn.Add(apiThing);
                    }
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in MoreComments method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }

            return toReturn;
        }

        public async Task<List<ApiMulti>> Multis()
        {
            List<ApiMulti> toReturn = [];

            try
            {
                await EnsureAuthenticated();

                var result = await _client.GetMyMultisAsync();

                if (result != null)
                {
                    toReturn = RedditModelMapper.Map(result);
                }
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }

            return toReturn;
        }

        public async Task SetUpvoteState(ApiThing thing, UpvoteState state)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                int direction = RedditModelMapper.MapUpvoteStateToInt(state);
                await _client.VoteAsync(thing.Name, direction);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in SetUpvoteState method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleDistinguish(ApiThing thing, bool distinguish, bool sticky)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                string how = distinguish ? "yes" : "no";
                await _client.DistinguishAsync(thing.Name, how, sticky);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in ToggleDistinguish method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleInboxReplies(ApiThing thing, bool enabled)
        {
            try
            {
                await EnsureAuthenticated();

                await _client.SetSendRepliesAsync(thing.Name, enabled);
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleLock(ApiThing thing, bool locked)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                if (locked)
                {
                    await _client.LockAsync(thing.Name);
                }
                else
                {
                    await _client.UnlockAsync(thing.Name);
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in ToggleLock method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleSave(ApiThing thing, bool saved)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                if (saved)
                {
                    await _client.SaveAsync(thing.Name);
                }
                else
                {
                    await _client.UnsaveAsync(thing.Name);
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in ToggleSave method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleSubScription(ApiSubReddit subreddit, bool subscribed)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                await _client.SubscribeAsync(subreddit.Name!, subscribed);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in ToggleSubscription method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleVisibility(ApiThing thing, bool visible)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                if (visible)
                {
                    await _client.UnhideAsync(thing.Name);
                }
                else
                {
                    await _client.HideAsync(thing.Name);
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in ToggleVisibility method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task<ApiComment?> Update(ApiThing thing)
        {
            try
            {
                await EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                var result = await _client.EditAsync(thing.Name, thing.Body);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Update method: {stopwatch.ElapsedMilliseconds}ms");

                return RedditModelMapper.Map(result);
            }
            catch (Exception ex)
            {
                if (!await DisplayException(ex))
                {
                    throw;
                }
                return null;
            }
        }

        public virtual Task<bool> DisplayException(Exception ex)
        {
            return Task.FromResult(false);
        }

        #region Private Methods

        private async Task TryAuthenticate()
        {
            if (_client.CanAuthenticate && !_client.IsAuthenticated)
            {
                await _client.AuthenticateAsync();
            }
        }

        private async Task EnsureAuthenticated()
        {
            if (!_client.IsAuthenticated)
            {
                if (_client.CanAuthenticate)
                {
                    if (!await _client.AuthenticateAsync())
                    {
                        throw new InvalidOperationException("Failed to authenticate");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Cannot authenticate - no valid credentials");
                }
            }
        }

        private static void SetParent(ApiThing parent, ApiThing child)
        {
            child.Parent = parent;

            if (child is ApiComment apiComment && apiComment.Replies != null)
            {
                foreach (var reply in apiComment.Replies.Children)
                {
                    SetParent(child, reply);
                }
            }
        }

        private void MapCommentReplies(ApiComment apiComment, Comment sourceComment, ApiThing parent)
        {
            if (sourceComment.Replies == null)
            {
                return;
            }

            // Replies can be empty string or a listing
            if (sourceComment.Replies is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    return;
                }

                // Try to parse as listing
                try
                {
                    var listing = System.Text.Json.JsonSerializer.Deserialize<Listing<Thing<Comment>>>(je.GetRawText());
                    if (listing?.Data?.Children != null)
                    {
                        apiComment.Replies = new ApiThingCollection
                        {
                            Children = []
                        };

                        foreach (var child in listing.Data.Children)
                        {
                            if (child?.Data == null)
                            {
                                continue;
                            }

                            if (child.Kind == "more")
                            {
                                var more = RedditModelMapper.MapMore(child.Data);
                                more.Parent = apiComment;
                                apiComment.Replies.Children.Add(more);
                            }
                            else
                            {
                                var reply = RedditModelMapper.Map(child.Data);
                                if (reply != null)
                                {
                                    reply.Parent = apiComment;
                                    MapCommentReplies(reply, child.Data, reply);
                                    apiComment.Replies.Children.Add(reply);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }
        }

        #endregion
    }
}
