using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Extensions;
using Deaddit.Core.Interfaces;
using Deaddit.Core.Reddit.Exceptions;
using Deaddit.Core.Reddit.Extensions;
using Deaddit.Core.Reddit.Interfaces;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Reddit.Models.Requests;
using Deaddit.Core.Reddit.Models.ThingDefinitions;
using Deaddit.Core.Utils.Extensions;
using Deaddit.Core.Utils.Validation;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Deaddit.Core.Reddit
{
    public class RedditClient : IRedditClient
    {
        private readonly IDisplayMessages _exceptionDisplay;

        private readonly HttpClient _httpClient;

        private readonly IJsonClient _jsonClient;

        private readonly RedditCredentials _redditCredentials;

        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private OAuthToken? _oAuthToken;

        private DateTime _tokenExpiration = DateTime.MinValue;

        public bool CanLogIn => _redditCredentials.Valid;

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(LoggedInUser);

        public DateTime LastFired { get; private set; } = DateTime.MinValue;

        public string? LoggedInUser { get; private set; }

        public int MinimumDelayMs { get; set; } = 500;

        private string ApiRoot => IsLoggedIn ? "https://oauth.reddit.com" : "https://www.reddit.com";

        public RedditClient(RedditCredentials redditCredentials, IJsonClient jsonClient, IDisplayMessages exceptionDisplay, HttpClient httpClient)
        {
            _redditCredentials = redditCredentials;
            _httpClient = httpClient;
            _jsonClient = jsonClient;
            _exceptionDisplay = exceptionDisplay;
            _jsonClient.SetDefaultHeader("User-Agent", "Deaddit");
        }

        // Default to 500ms
        public async Task<ApiSubReddit?> About(SubRedditDefinition subreddit)
        {
            try
            {
                await this.TryAuthenticate();
                await this.ThrottleAsync();

                string fullUrl = $"{ApiRoot}/r/{subreddit.Name}/about";

                if (!IsLoggedIn)
                {
                    fullUrl += ".json";
                }

                Stopwatch stopwatch = new();
                stopwatch.Start();

                ApiSubReddit response = (ApiSubReddit)await _jsonClient.Get<ApiThing>(fullUrl);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in About method: {stopwatch.ElapsedMilliseconds}ms");

                return response;
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task Message(ApiUser thing, string subject, string body)
        {
            try
            {
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();
                stopwatch.Start();

                string fullUrl = $"{ApiRoot}/api/compose";

                // Prepare the form values as a dictionary
                Dictionary<string, string> formValues = new()
                {
                    { "api_type", "json" },
                    { "subject", subject },
                    { "text", body },
                    { "to", thing.Name }
                };

                // Use the modified Post method to send the form data
                PostCommentResponse response = await _jsonClient.Post<PostCommentResponse>(fullUrl, formValues);

                if (response.Json.Errors.Count > 0)
                {
                    List<Exception> exceptions = [];

                    foreach (string error in response.Json.Errors)
                    {
                        exceptions.Add(new Exception(error));
                    }

                    throw new AggregateException(exceptions);
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Comment method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task<ApiComment> Comment(ApiThing thing, string comment)
        {
            try
            {
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();
                stopwatch.Start();

                string fullUrl = $"{ApiRoot}/api/comment";

                // Prepare the form values as a dictionary
                Dictionary<string, string> formValues = new()
                {
                    { "api_type", "json" },
                    { "thing_id", thing.Name },
                    { "text", comment }
                };

                // Use the modified Post method to send the form data
                PostCommentResponse response = await _jsonClient.Post<PostCommentResponse>(fullUrl, formValues);

                if (response.Json.Errors.Count > 0)
                {
                    List<Exception> exceptions = [];

                    foreach (string error in response.Json.Errors)
                    {
                        exceptions.Add(new Exception(error));
                    }

                    throw new AggregateException(exceptions);
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Comment method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");

                return response.Json.Data.Things.OfType<ApiComment>().Single();
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<List<ApiThing>> Comments(ApiPost parent, ApiComment? focusComment)
        {
            List<ApiThingCollection> response;
            ApiThing responseParent = (ApiThing)focusComment ?? parent;
            Stopwatch stopwatch = new();
            List<ApiThing> toReturn = [];

            try
            {
                await this.TryAuthenticate();
                await this.ThrottleAsync();

                stopwatch.Start();

                string fullUrl = $"{ApiRoot}/comments/{parent.Id}";

                if (!IsLoggedIn)
                {
                    //Hack for not logged in
                    fullUrl = fullUrl.Replace("://oauth.", "://www.") + ".json";
                }

                if (!string.IsNullOrWhiteSpace(focusComment?.Id))
                {
                    fullUrl += $"?comment={focusComment?.Id}";
                }

                response = await _jsonClient.Get<List<ApiThingCollection>>(fullUrl);

                foreach (ApiThingCollection commentReadResponse in response)
                {
                    if (commentReadResponse?.Children != null)
                    {
                        foreach (ApiThing child in commentReadResponse.Children)
                        {
                            SetParent(responseParent, child);
                        }
                    }
                }

                foreach (ApiThingCollection commentReadResponse in response)
                {
                    if (commentReadResponse?.Children != null)
                    {
                        foreach (ApiThing child in commentReadResponse.Children)
                        {
                            if (child.Id != parent.Id)
                            {
                                toReturn.Add(child);
                            }
                        }
                    }
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Comments method: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
            }

            return toReturn;
        }

        public async Task Delete(ApiThing thing)
        {
            try
            {
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();
                stopwatch.Start();

                string url = $"{ApiRoot}/api/del";

                // Prepare the form values as a dictionary
                Dictionary<string, string> formValues = new()
                {
                    { "id", thing.Name }
                };

                await _jsonClient.Post(url, formValues);

                stopwatch.Stop();

                Debug.WriteLine($"[DEBUG] Time spent in Delete method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
            }
        }

        public async Task<Dictionary<string, UserPartial>> GetPartialUserData(IEnumerable<string> usernames)
        {
            try
            {
                Ensure.NotNull(usernames);

                List<string> userNames = usernames.ToList();

                if (userNames.Count == 0)
                {
                    return [];
                }

                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();
                stopwatch.Start();

                string url = $"{ApiRoot}/api/user_data_by_account_ids?ids={string.Join(",", userNames)}";

                try
                {
                    Dictionary<string, UserPartial> response = await _jsonClient.Get<Dictionary<string, UserPartial>>(url);
                    return response;
                }
                catch (HttpRequestException hre) when (hre.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Debug.WriteLine($"User data not found ('{string.Join(",", userNames)}')");
                    return [];
                }
                finally
                {
                    stopwatch.Stop();
                    Debug.WriteLine($"[DEBUG] Time spent in GetUserData method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");
                }
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
                else
                {
                    return [];
                }
            }
        }

        public async Task<ApiPost> GetPost(string id)
        {
            try
            {
                // Ensure the user is authenticated before making the request
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();
                stopwatch.Start();

                // Format the ID to include the 't3_' prefix if not already present
                string fullname = id.StartsWith("t3_") ? id : $"t3_{id}";

                // Construct the full URL for the API request
                string fullUrl = $"{ApiRoot}/api/info?id={fullname}";

                // Make the API call to get the post data
                ApiThingCollection response = await _jsonClient.Get<ApiThingCollection>(fullUrl);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in GetPost method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");

                // Check if the response contains any data
                if (response.Children.NotNullAny())
                {
                    // Retrieve the first item from the response
                    ApiThing apiThing = response.Children.First();

                    // Return the ApiPost object from the retrieved data
                    return apiThing as ApiPost;
                }
                else
                {
                    // Return null if no data is found
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions using the provided exception display mechanism
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<List<ApiThing>> GetPosts<T>(ApiEndpointDefinition endpointDefinition, T sort, int pageSize, string? after = null, Models.Region region = Models.Region.GLOBAL) where T : Enum
        {
            //TODO: This makes way more sense as an IEnumerable
            //Remove page size and fix the interop between try/catch
            //and the async functionality.

            List<ApiThing> toReturn = [];

            try
            {
                await this.TryAuthenticate();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();

                stopwatch.Start();

                do
                {
                    string fullUrl = endpointDefinition.WithRoot(ApiRoot)
                                                       .WithSort(sort)
                                                       .WithCallingUser(LoggedInUser)
                                                       .WithParam("after", after)
                                                       .WithParam("g", region)
                                                       .Url;

                    //Modify to use the public non-api url if not logged in
                    if (!IsLoggedIn)
                    {
                        Uri uri = new(fullUrl);
                        fullUrl = ApiRoot + uri.AbsolutePath + ".json" + uri.Query;
                    }

                    ApiThingCollection posts = await _jsonClient.Get<ApiThingCollection>(fullUrl);

                    stopwatch.Stop();
                    Debug.WriteLine($"[DEBUG] Time spent in GetPosts method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");

                    if (!posts.Children.NotNullAny())
                    {
                        return toReturn;
                    }

                    foreach (ApiThing redditPostMeta in posts.Children)
                    {
                        toReturn.Add(redditPostMeta);

                        if (toReturn.Count >= pageSize)
                        {
                            return toReturn;
                        }

                        after = redditPostMeta.Name;
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
                else
                {
                    return toReturn;
                }
            }
        }

        public async Task<Stream> GetStream(string url)
        {
            await this.EnsureAuthenticated();
            await this.ThrottleAsync();

            return await _httpClient.GetStreamAsync(url);
        }

        public async Task<ApiUser> GetUserData(string username)
        {
            await this.TryAuthenticate();
            await this.ThrottleAsync();

            string fullUrl = $"{ApiRoot}/user/{username}/about";

            if (!IsLoggedIn)
            {
                fullUrl += ".json";
            }

            return (ApiUser)await _jsonClient.Get<ApiThing>(fullUrl);
        }

        public async Task MarkRead(ApiThing message, bool state)
        {
            await this.SimpleToggle(message, state, $"{ApiRoot}/api/read_message", $"{ApiRoot}/api/unread_message");
        }

        public async Task<List<ApiThing>> MoreComments(ApiPost post, IMore moreItem)
        {
            List<ApiThing> toReturn = [];

            try
            {
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                // Exclude authentication or other setup time if necessary
                Stopwatch stopwatch = new();
                stopwatch.Start();

                // Ensure the required properties are not null or empty
                Ensure.NotNullOrEmpty(moreItem.ChildNames);

                string fullUrl = $"{ApiRoot}/api/morechildren";

                // Prepare the form values as a dictionary
                Dictionary<string, string> formValues = new()
                {
                    { "api_type", "json" },
                    { "link_id", post.Name },
                    { "children", string.Join(",", moreItem.ChildNames) },
                    { "limit_children", "false" },
                    { "depth", "999" }
                };

                // Use the modified Post method to send the form data
                MoreCommentsResponse response = await _jsonClient.Post<MoreCommentsResponse>(fullUrl, formValues);

                List<ApiThing> things = [.. response.Json.Data.Things];

                Dictionary<string, ApiComment> tree = [];

                foreach (ApiComment redditCommentMeta in things.OfType<ApiComment>())
                {
                    tree.Add(redditCommentMeta.Name, redditCommentMeta);
                }

                foreach (ApiThing redditCommentMeta in things.ToList())
                {
                    if (redditCommentMeta is ApiComment apiComment)
                    {
                        if (apiComment?.ParentId is null)
                        {
                            continue;
                        }

                        if (tree.TryGetValue(apiComment.ParentId, out ApiComment? parent))
                        {
                            parent.AddReply(redditCommentMeta);
                            things.Remove(redditCommentMeta);
                        }
                    }

                    if (redditCommentMeta is ApiMore apiMore)
                    {
                        if (apiMore?.ParentId is null)
                        {
                            continue;
                        }

                        if (tree.TryGetValue(apiMore.ParentId, out ApiComment? parent))
                        {
                            parent.AddReply(redditCommentMeta);
                            things.Remove(redditCommentMeta);
                        }
                    }
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in MoreComments method: {stopwatch.ElapsedMilliseconds}ms");

                foreach (ApiThing redditCommentMeta in things)
                {
                    if (moreItem.Parent is null)
                    {
                        continue;
                    }

                    SetParent(moreItem.Parent, redditCommentMeta);

                    toReturn.Add(redditCommentMeta);
                }
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
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
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                string url = $"{ApiRoot}/api/multi/mine";

                List<ApiMultiMeta> response = await _jsonClient.Get<List<ApiMultiMeta>>(url);

                foreach (ApiMultiMeta multi in response)
                {
                    toReturn.Add(multi.Data);
                }
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
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
                // Exclude authentication time
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();
                stopwatch.Start();

                int stateInt = 0;

                switch (state)
                {
                    case UpvoteState.Upvote:
                        stateInt = 1;
                        break;

                    case UpvoteState.Downvote:
                        stateInt = -1;
                        break;

                    case UpvoteState.None:
                        stateInt = 0;
                        break;
                }

                string url = $"{ApiRoot}/api/vote?dir={stateInt}&id={thing.Name}";

                await _jsonClient.Post(url);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in SetUpvoteState method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
                else
                {
                    return;
                }
            }
        }

        public async Task ThrottleAsync()
        {
            await _semaphore.WaitAsync();

            try
            {
                TimeSpan elapsedSinceLastFire = DateTime.UtcNow - LastFired;
                TimeSpan remainingDelay = TimeSpan.FromMilliseconds(MinimumDelayMs) - elapsedSinceLastFire;

                if (remainingDelay > TimeSpan.Zero)
                {
                    await Task.Delay(remainingDelay);
                }

                LastFired = DateTime.UtcNow;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ToggleInboxReplies(ApiThing thing, bool enabled)
        {
            await this.SimpleToggle(thing, enabled, $"{ApiRoot}/api/sendreplies", $"{ApiRoot}/api/sendreplies");
        }

        public async Task ToggleSave(ApiThing thing, bool saved)
        {
            await this.SimpleToggle(thing, saved, $"{ApiRoot}/api/save", $"{ApiRoot}/api/unsave");
        }

        public async Task ToggleSubScription(ApiSubReddit thing, bool subscribed)
        {
            try
            {
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();
                stopwatch.Start();

                string url = $"{ApiRoot}/api/subscribe";

                // Prepare the form values as a dictionary
                Dictionary<string, string> formValues = new()
                {
                    { "action", subscribed ? "sub" : "unsub" },
                    { "sr", $"{thing.Name}" }
                };

                await _jsonClient.Post(url, formValues);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in ToggleSubScription method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
                else
                {
                    return;
                }
            }
        }

        public async Task ToggleVisibility(ApiThing thing, bool visible)
        {
            await this.SimpleToggle(thing, visible, $"{ApiRoot}/api/unhide", $"{ApiRoot}/api/hide");
        }

        public async Task<ApiComment> Update(ApiThing thing)
        {
            try
            {
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();
                stopwatch.Start();

                string fullUrl = $"{ApiRoot}/api/editusertext";

                // Prepare the form values as a dictionary
                Dictionary<string, string> formValues = new()
                {
                    { "api_type", "json" },
                    { "thing_id", thing.Name },
                    { "text", thing.Body }
                };

                // Use the modified Post method to send the form data
                PostCommentResponse response = await _jsonClient.Post<PostCommentResponse>(fullUrl, formValues);

                if (response.Json.Errors.Count > 0)
                {
                    List<Exception> exceptions = [];

                    foreach (string error in response.Json.Errors)
                    {
                        exceptions.Add(new Exception(error));
                    }

                    throw new AggregateException(exceptions);
                }

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in Comment method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");

                return response.Json.Data.Things.OfType<ApiComment>().Single();
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
                else
                {
                    return null;
                }
            }
        }

        private static void SetParent(ApiThing parent, ApiThing apiThing)
        {
            apiThing.Parent = parent;

            if (apiThing is ApiComment apiComment)
            {
                if (apiComment.Replies is null)
                {
                    return;
                }

                foreach (ApiThing child in apiComment.Replies.Children)
                {
                    SetParent(apiThing, child);
                }
            }
        }

        private async Task EnsureAuthenticated()
        {
            if (!await this.TryAuthenticate())
            {
                throw new InvalidCredentialsException();
            }
        }

        private async Task SimpleToggle(ApiThing thing, bool state, string trueUrl, string falseUrl, [CallerMemberName] string logName = null)
        {
            try
            {
                await this.EnsureAuthenticated();
                await this.ThrottleAsync();

                Stopwatch stopwatch = new();

                stopwatch.Start();

                string url = !state ? falseUrl : trueUrl;

                // Prepare the form values as a dictionary
                Dictionary<string, string> formValues = new()
                {
                    { "id", thing.Name },
                    { "state", $"{state}" }
                };

                await _jsonClient.Post(url, formValues);

                stopwatch.Stop();
                Debug.WriteLine($"[DEBUG] Time spent in {logName} method (excluding authentication): {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                if (!await _exceptionDisplay.DisplayException(ex))
                {
                    throw;
                }
                else
                {
                    return;
                }
            }
        }

        private async Task<bool> TryAuthenticate()
        {
            if (_tokenExpiration < DateTime.Now)
            {
                _oAuthToken = null;
                LoggedInUser = string.Empty;
            }

            if (_oAuthToken is null)
            {
                if (!_redditCredentials.Valid)
                {
                    return false;
                }

                Ensure.NotNullOrWhiteSpace(_redditCredentials.UserName);
                Ensure.NotNullOrWhiteSpace(_redditCredentials.Password);
                Ensure.NotNullOrWhiteSpace(_redditCredentials.AppKey);
                Ensure.NotNullOrWhiteSpace(_redditCredentials.AppSecret);

                string text = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(_redditCredentials.AppKey + ":" + _redditCredentials.AppSecret));

                // Set the Authorization header
                _httpClient.SetDefaultHeader("Authorization", "Basic " + text);

                // Encode the form values
                string encodedUsername = Uri.EscapeDataString(_redditCredentials.UserName);
                string encodedPassword = Uri.EscapeDataString(_redditCredentials.Password);

                // Prepare the content with encoded values
                StringContent content = new($"grant_type=password&username={encodedUsername}&password={encodedPassword}",
                    Encoding.UTF8, "application/x-www-form-urlencoded");

                // Make the POST request
                HttpResponseMessage response = await _httpClient.PostAsync($"{ApiRoot}/api/v1/access_token", content);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                // Read and deserialize the response content
                string responseContent = await response.Content.ReadAsStringAsync();

                _oAuthToken = JsonSerializer.Deserialize<OAuthToken>(responseContent)!;

                _tokenExpiration = DateTime.Now.AddSeconds(_oAuthToken.ExpiresIn - 5);

                _jsonClient.SetDefaultHeader("Authorization", _oAuthToken.TokenType + " " + _oAuthToken.AccessToken);

                LoggedInUser = _redditCredentials.UserName;
            }

            return true;
        }
    }
}