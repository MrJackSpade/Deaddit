using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Mapping;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.Requests;
using Deaddit.Core.Reddit.Models.ThingDefinitions;
using Reddit.Api.Models.Enums;
using Reddit.Api.Models.Json.Common;
using Reddit.Api.Models.Json.LinksComments;
using Reddit.Api.Models.Json.Listings;
using Reddit.Api.Models.Json.Media;
using Reddit.Api.Models.Json.Multis;
using Reddit.Api.Models.Json.Subreddits;
using Reddit.Api.Models.Json.Search;
using Reddit.Api.Models.Json.Users;
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

        public RedditService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _client = new NewClient.RedditClient(new NewClient.RedditCredentials(), httpClient);
        }

        public RedditService(IRedditCredentials credentials, HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            NewClient.RedditCredentials clientCredentials = new()
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

        public void SetTokenRefreshFunction(Func<Task<string?>> tokenRefreshFunc)
        {
            _client.SetTokenRefreshFunction(tokenRefreshFunc);
        }

        public async Task<Subreddit?> About(SubRedditDefinition subreddit)
        {
            try
            {
                await this.TryAuthenticate();

                Stopwatch stopwatch = Stopwatch.StartNew();

                Thing<Subreddit>? result = await _client.GetSubredditAboutAsync(subreddit.Name);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in About method: {stopwatch.ElapsedMilliseconds}ms");

                return result?.Data;
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }

                return null;
            }
        }

        public async Task<ApiComment?> Comment(ApiThing replyTo, string markdown)
        {
            try
            {
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                RichTextDocument richtext = MarkdownRichTextConverter.Convert(markdown);
                Thing<Comment>? result = await _client.CommentAsync(replyTo.Name, richtext);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Comment method: {stopwatch.ElapsedMilliseconds}ms");

                return RedditModelMapper.Map(result);
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }

                return null;
            }
        }

        public async Task<string> UploadMedia(Stream fileStream, string filename, string mimetype)
        {
            await this.EnsureAuthenticated();

            MediaAssetResponse? lease = await _client.GetMediaAssetUploadLeaseAsync(filename, mimetype);

            if (lease == null)
            {
                throw new InvalidOperationException("Failed to get media upload lease");
            }

            string mediaKey = lease.Args.Fields.First(f => f.Name == "key").Value;

            bool uploaded = await _client.UploadMediaToS3Async(lease, fileStream, filename, mimetype);

            if (!uploaded)
            {
                throw new InvalidOperationException("Failed to upload media to S3");
            }

            return mediaKey;
        }

        public async Task<List<ApiThing>> Comments(ApiPost post, CommentFocus? focus = null)
        {
            List<ApiThing> toReturn = [];

            try
            {
                await this.TryAuthenticate();

                Stopwatch stopwatch = Stopwatch.StartNew();

                (Thing<Link> _, Listing<Thing<Comment>> comments) = await _client.GetCommentsAsync(
                    post.Id!,
                    focus?.Comment?.Id,
                    sort: null,
                    limit: null,
                    depth: null,
                    context: focus?.Context);

                ApiThing responseParent = (ApiThing?)focus?.Comment ?? post;

                if (comments?.Data?.Children != null)
                {
                    foreach (Thing<Comment> child in comments.Data.Children)
                    {
                        if (child?.Data == null)
                        {
                            continue;
                        }

                        // Check if this is a "more" item
                        if (child.Kind == ThingKind.More)
                        {
                            ApiMore more = RedditModelMapper.MapMore(child.Data);
                            more.Parent = responseParent;
                            toReturn.Add(more);
                        }
                        else
                        {
                            ApiComment apiComment = RedditModelMapper.Map(child.Data);
                            if (apiComment != null && apiComment.Id != post.Id)
                            {
                                apiComment.Parent = responseParent;
                                this.MapCommentReplies(apiComment, child.Data, apiComment);
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
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }

            return toReturn;
        }

        public async Task<ApiPost?> CreatePost(string subreddit, string title, string content, SubmitKind kind)
        {
            try
            {
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                SubmitRequest request = new()
                {
                    Subreddit = subreddit,
                    Title = title,
                    Kind = kind
                };

                if (kind is SubmitKind.Self)
                {
                    request.Text = content;
                }
                else if (kind == SubmitKind.Link)
                {
                    request.Url = content;
                }

                SubmitResponseData? result = await _client.SubmitAsync(request);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in CreatePost method: {stopwatch.ElapsedMilliseconds}ms");

                if (result?.Name != null)
                {
                    return await this.GetPost(result.Name);
                }

                return null;
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
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
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                await _client.DeleteThingAsync(thing.Name);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Delete method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public virtual Task<bool> DisplayException(Exception ex)
        {
            return Task.FromResult(false);
        }

        public async Task<Dictionary<string, UserPartialData>> GetPartialUserData(IEnumerable<string> usernames)
        {
            try
            {
                List<string> usernameList = usernames.Where(u => !string.IsNullOrWhiteSpace(u)).ToList();
                if (usernameList.Count == 0)
                {
                    return [];
                }

                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                global::Reddit.Api.Models.Json.Users.UserDataByIdsResponse? result = await _client.GetUserDataByIdsAsync(usernameList);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in GetPartialUserData method: {stopwatch.ElapsedMilliseconds}ms");

                if (result == null || result.Count == 0)
                {
                    return [];
                }

                return new Dictionary<string, UserPartialData>(result);
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
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
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                string fullname = id.StartsWith("t3_") ? id : $"t3_{id}";

                Listing<Thing<Link>>? result = await _client.GetInfoAsync([fullname]);

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
                if (!await this.DisplayException(ex))
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
                await this.EnsureAuthenticated();

                IEnumerable<string> fullnames = ids.Select(id => id.StartsWith("t3_") ? id : $"t3_{id}");

                Listing<Thing<Link>>? result = await _client.GetByIdAsync(fullnames);

                return RedditModelMapper.MapPostsTyped(result);
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
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
                await this.TryAuthenticate();

                Stopwatch stopwatch = Stopwatch.StartNew();

                ListingParameters parameters = new()
                {
                    After = after,
                    Limit = Math.Min(100, pageSize),
                    Geo = region != Region.GLOBAL ? region.ToString() : null
                };

                string endpoint = endpointDefinition.Url.TrimStart('/');

                // Replace %USER% placeholder with actual username
                if (endpoint.Contains("%USER%"))
                {
                    endpoint = endpoint.Replace("%USER%", _client.AuthenticatedUser ?? throw new InvalidOperationException("Must be logged in to view this content"));
                }

                // Build the full URL dynamically based on endpoint and sort
                string fullEndpoint;

                if (sort != null)
                {
                    string sortName = sort.ToString().ToLower();

                    if (string.IsNullOrEmpty(endpoint))
                    {
                        // Front page
                        fullEndpoint = $"/{sortName}";
                    }
                    else
                    {
                        // Subreddit or user endpoint - append sort
                        fullEndpoint = $"/{endpoint}/{sortName}";
                    }
                }
                else
                {
                    // No sort - use endpoint as-is
                    fullEndpoint = string.IsNullOrEmpty(endpoint) ? "/" : $"/{endpoint}";
                }

                Listing<Thing<object>>? result = await _client.GetListingAsync<object>(fullEndpoint, parameters);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in GetPosts method: {stopwatch.ElapsedMilliseconds}ms");

                toReturn = RedditModelMapper.MapThings(result);

                // Fallback to search API for empty user profiles
                if (toReturn.Count == 0 && after == null && endpoint.StartsWith("user/") && !fullEndpoint.EndsWith("/comments"))
                {
                    string username = endpoint.Split('/')[1];

                    Debug.WriteLine($"[DEBUG] User profile empty for {username}, falling back to search API");

                    SearchParameters searchParams = new()
                    {
                        Query = $"author:{username}",
                        Sort = SearchSort.New,
                        Time = "all",
                        Limit = Math.Min(100, pageSize)
                    };

                    Listing<Thing<Link>>? searchResult = await _client.SearchAsync(searchParams);

                    if (searchResult != null)
                    {
                        toReturn = RedditModelMapper.MapPostsTyped(searchResult).Cast<ApiThing>().ToList();
                    }
                }

                // Trim to page size if needed
                if (toReturn.Count > pageSize)
                {
                    toReturn = toReturn.Take(pageSize).ToList();
                }
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }

            return toReturn;
        }

        public async Task<Stream> GetStream(string url)
        {
            await this.EnsureAuthenticated();
            return await _httpClient.GetStreamAsync(url);
        }

        public async Task<User?> GetUserData(string username)
        {
            try
            {
                await this.TryAuthenticate();

                Thing<User>? result = await _client.GetUserAboutAsync(username);
                return result?.Data;
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
                await this.EnsureAuthenticated();

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
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task Message(User user, string subject, string body)
        {
            try
            {
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                await _client.ComposeMessageAsync(user.Name!, subject, body);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Message method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
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
                await this.EnsureAuthenticated();

                if (moreItem.ChildNames == null || moreItem.ChildNames.Count == 0)
                {
                    return toReturn;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();

                List<Thing<Comment>>? result = await _client.GetMoreChildrenAsync(
                    post.Name,
                    moreItem.ChildNames,
                    sort: null,
                    depth: 999);

                if (result != null)
                {
                    // Build tree structure
                    Dictionary<string, ApiComment> tree = [];

                    foreach (Thing<Comment> thing in result)
                    {
                        if (thing?.Data != null && thing.Kind != ThingKind.More)
                        {
                            ApiComment comment = RedditModelMapper.Map(thing.Data);
                            if (comment != null)
                            {
                                tree[comment.Name] = comment;
                            }
                        }
                    }

                    // Link parents
                    List<ApiThing> allThings = [];
                    foreach (Thing<Comment> thing in result)
                    {
                        if (thing?.Data == null)
                        {
                            continue;
                        }

                        if (thing.Kind == ThingKind.More)
                        {
                            ApiMore more = RedditModelMapper.MapMore(thing.Data);
                            allThings.Add(more);
                        }
                        else if (tree.TryGetValue(thing.Data.Name, out ApiComment? comment))
                        {
                            allThings.Add(comment);
                        }
                    }

                    // Nest children under parents
                    foreach (ApiThing? apiThing in allThings.ToList())
                    {
                        if (apiThing is ApiComment apiComment)
                        {
                            if (apiComment.ParentId != null && tree.TryGetValue(apiComment.ParentId, out ApiComment? parent))
                            {
                                parent.AddReply(apiComment);
                                allThings.Remove(apiThing);
                            }
                        }

                        if (apiThing is ApiMore apiMore)
                        {
                            if (apiMore.ParentId != null && tree.TryGetValue(apiMore.ParentId, out ApiComment? parent))
                            {
                                parent.AddReply(apiMore);
                                allThings.Remove(apiThing);
                            }
                        }
                    }

                    // Set parent for remaining top-level items
                    foreach (ApiThing apiThing in allThings)
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
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }

            return toReturn;
        }

        public async Task<bool> AddSubredditToMulti(Multi multi, string subreddit)
        {
            try
            {
                await this.EnsureAuthenticated();

                string multipath = multi.Path.TrimStart('/');

                return await _client.AddSubredditToMultiAsync(multipath, subreddit);
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }

            return false;
        }

        public async Task<Multi?> CreateMulti(string name)
        {
            try
            {
                await this.EnsureAuthenticated();

                string username = _client.AuthenticatedUser ?? throw new InvalidOperationException("Must be logged in to create a multi");
                string multipath = $"user/{username}/m/{name}";

                MultiCreateRequest request = new()
                {
                    DisplayName = name,
                    Visibility = "private"
                };

                MultiResponse? result = await _client.CreateOrUpdateMultiAsync(multipath, request);

                return result?.Data;
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }

            return null;
        }

        public async Task<bool> DeleteMulti(Multi multi)
        {
            try
            {
                await this.EnsureAuthenticated();

                string multipath = multi.Path.TrimStart('/');

                return await _client.DeleteMultiAsync(multipath);
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }

            return false;
        }

        public async Task<List<Multi>> Multis()
        {
            List<Multi> toReturn = [];

            try
            {
                await this.EnsureAuthenticated();

                List<MultiResponse>? result = await _client.GetMyMultisAsync();

                if (result != null)
                {
                    toReturn = result.Where(r => r.Data != null).Select(r => r.Data!).ToList();
                }
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }

            return toReturn;
        }

        public async Task<bool> RemoveSubredditFromMulti(Multi multi, string subreddit)
        {
            try
            {
                await this.EnsureAuthenticated();

                string multipath = multi.Path.TrimStart('/');

                return await _client.RemoveSubredditFromMultiAsync(multipath, subreddit);
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }

            return false;
        }

        public async Task SetVoteState(ApiThing thing, VoteState state)
        {
            try
            {
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                await _client.VoteAsync(thing.Name, (int)state);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in SetVoteState method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleDistinguish(ApiThing thing, bool distinguish, bool sticky)
        {
            try
            {
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                DistinguishHow how = distinguish ? DistinguishHow.Yes : DistinguishHow.No;
                await _client.DistinguishAsync(thing.Name, how, sticky);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in ToggleDistinguish method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleInboxReplies(ApiThing thing, bool enabled)
        {
            try
            {
                await this.EnsureAuthenticated();

                await _client.SetSendRepliesAsync(thing.Name, enabled);
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleLock(ApiThing thing, bool locked)
        {
            try
            {
                await this.EnsureAuthenticated();

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
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleSave(ApiThing thing, bool saved)
        {
            try
            {
                await this.EnsureAuthenticated();

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
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleSubScription(Subreddit subreddit, bool subscribed)
        {
            try
            {
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                await _client.SubscribeAsync(subreddit.Name!, subscribed);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in ToggleSubscription method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task ToggleVisibility(ApiThing thing, bool visible)
        {
            try
            {
                await this.EnsureAuthenticated();

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
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task<ApiComment?> Update(ApiThing thing)
        {
            try
            {
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                RichTextDocument richtext = MarkdownRichTextConverter.Convert(thing.Body);
                Thing<Comment>? result = await _client.EditAsync(thing.Name, richtext);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Update method: {stopwatch.ElapsedMilliseconds}ms");

                return RedditModelMapper.Map(result);
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }

                return null;
            }
        }

        public async Task Report(ApiThing thing, string? reason = null, string? siteReason = null, string? ruleReason = null)
        {
            try
            {
                await this.EnsureAuthenticated();

                Stopwatch stopwatch = Stopwatch.StartNew();

                await _client.ReportAsync(thing.Name, reason, siteReason, ruleReason);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Report method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task<SubredditRulesResponse?> GetSubredditRules(string subreddit)
        {
            try
            {
                await this.TryAuthenticate();

                Stopwatch stopwatch = Stopwatch.StartNew();

                SubredditRulesResponse? result = await _client.GetSubredditRulesAsync(subreddit);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in GetSubredditRules method: {stopwatch.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                if (!await this.DisplayException(ex))
                {
                    throw;
                }

                return null;
            }
        }

        #region Private Methods

        private static void SetParent(ApiThing parent, ApiThing child)
        {
            child.Parent = parent;

            if (child is ApiComment apiComment && apiComment.Replies != null)
            {
                foreach (ApiThing reply in apiComment.Replies.Children)
                {
                    SetParent(child, reply);
                }
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
                    Listing<Thing<Comment>>? listing = System.Text.Json.JsonSerializer.Deserialize<Listing<Thing<Comment>>>(je.GetRawText());
                    if (listing?.Data?.Children != null)
                    {
                        apiComment.Replies = new ApiThingCollection
                        {
                            Children = []
                        };

                        foreach (Thing<Comment> child in listing.Data.Children)
                        {
                            if (child?.Data == null)
                            {
                                continue;
                            }

                            if (child.Kind == ThingKind.More)
                            {
                                ApiMore more = RedditModelMapper.MapMore(child.Data);
                                more.Parent = apiComment;
                                apiComment.Replies.Children.Add(more);
                            }
                            else
                            {
                                ApiComment reply = RedditModelMapper.Map(child.Data);
                                if (reply != null)
                                {
                                    reply.Parent = apiComment;
                                    this.MapCommentReplies(reply, child.Data, reply);
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

        private async Task TryAuthenticate()
        {
            if (_client.CanAuthenticate && !_client.IsAuthenticated)
            {
                await _client.AuthenticateAsync();
            }
        }

        #endregion Private Methods
    }
}