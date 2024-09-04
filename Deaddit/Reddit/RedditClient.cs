using Deaddit.Configurations.Models;
using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Reddit.Exceptions;
using Deaddit.Reddit.Extensions;
using Deaddit.Reddit.Interfaces;
using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;
using System.Text;
using System.Text.Json;

namespace Deaddit.Reddit
{
    internal class RedditClient : IRedditClient
    {
        private const string API_ROOT = "https://oauth.reddit.com";

        private const string AUTHORIZATION_ROOT = "https://www.reddit.com";

        private readonly HttpClient _httpClient;

        private readonly IJsonClient _jsonClient;

        private readonly RedditCredentials _redditCredentials;

        private OAuthToken? _oAuthToken;

        public RedditClient(RedditCredentials redditCredentials, IJsonClient jsonClient, HttpClient httpClient)
        {
            _redditCredentials = redditCredentials;
            _httpClient = httpClient;
            _jsonClient = jsonClient;
            _jsonClient.SetDefaultHeader("User-Agent", "Deaddit");
        }

        public string LoggedInUser { get; private set; }

        public async Task<RedditCommentMeta> Comment(ApiThing thing, string comment)
        {
            Ensure.NotNullOrWhiteSpace(thing.Name);

            await this.EnsureAuthenticated();

            string fullUrl = $"{API_ROOT}/api/comment";

            // Prepare the form values as a dictionary
            Dictionary<string, string> formValues = new()
            {
                { "api_type", "json" },
                { "thing_id", thing.Name },
                { "text", comment }
            };

            // Use the modified Post method to send the form data
            PostCommentResponse response = await _jsonClient.Post<PostCommentResponse>(fullUrl, formValues);

            if (response.Json?.Data is null)
            {
                throw new InvalidApiResponseException("API returned an invalid response");
            }

            if (response.Json.Errors.Count > 0)
            {
                List<Exception> exceptions = [];

                foreach (string error in response.Json.Errors)
                {
                    exceptions.Add(new Exception(error));
                }

                throw new AggregateException([.. exceptions]);
            }

            return response.Json.Data.Things.Single();
        }

        public async IAsyncEnumerable<RedditCommentMeta> Comments(ApiPost parent, string comment)
        {
            string fullUrl = $"{API_ROOT}/comments/{parent.Id}";

            if (!string.IsNullOrWhiteSpace(comment))
            {
                fullUrl += $"?comment={comment}";
            }

            List<CommentReadResponse> response = await _jsonClient.Get<List<CommentReadResponse>>(fullUrl);

            foreach (CommentReadResponse commentReadResponse in response)
            {
                if (commentReadResponse.Data?.Children != null)
                {
                    foreach (RedditCommentMeta child in commentReadResponse.Data.Children)
                    {
                        SetParent(parent, child);
                    }
                }
            }

            foreach (CommentReadResponse commentReadResponse in response)
            {
                if (commentReadResponse.Data?.Children != null)
                {
                    foreach (RedditCommentMeta child in commentReadResponse.Data.Children)
                    {
                        if (child.Data is null)
                        {
                            continue;
                        }

                        if (child.Data.Id != parent.Id)
                        {
                            yield return child;
                        }
                    }
                }
            }
        }

        public async Task<Stream> GetStream(string url)
        {
            await this.EnsureAuthenticated();

            return await _httpClient.GetStreamAsync(url);
        }

        public async IAsyncEnumerable<RedditCommentMeta> MoreComments(ApiPost post, ApiComment moreItem)
        {
            // Ensure the required properties are not null or empty
            Ensure.NotNullOrWhiteSpace(post.Name);
            Ensure.NotNullOrEmpty(moreItem.ChildNames);

            await this.EnsureAuthenticated();

            string fullUrl = $"{API_ROOT}/api/morechildren";

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

            if (response?.Json?.Data is null)
            {
                yield break;
            }

            List<RedditCommentMeta> things = response.Json.Data.Things;

            Dictionary<string, RedditCommentMeta> tree = [];

            foreach (RedditCommentMeta redditCommentMeta in things)
            {
                tree.Add(redditCommentMeta.Data.Name, redditCommentMeta);
            }

            foreach (RedditCommentMeta redditCommentMeta in things.ToList())
            {
                if (tree.TryGetValue(redditCommentMeta.Data.ParentId, out RedditCommentMeta parent))
                {
                    parent.AddReply(redditCommentMeta);
                    things.Remove(redditCommentMeta);
                }
            }

            foreach (RedditCommentMeta redditCommentMeta in things)
            {
                SetParent(moreItem.Parent, redditCommentMeta);

                yield return redditCommentMeta;
            }
        }

        public async IAsyncEnumerable<ApiPost> Read(string subreddit, string? sort = null, string? after = null, Models.Region region = Models.Region.GLOBAL)
        {
            await this.EnsureAuthenticated();

            if (subreddit.Length > 0 && subreddit[0] != '/')
            {
                subreddit = $"/{subreddit}";
            }

            sort = FixSort(sort);

            string fullUrl = $"{API_ROOT}{subreddit}{sort}?after={after}&g={region}";

            do
            {
                SubRedditReadResponse posts = await _jsonClient.Get<SubRedditReadResponse>(fullUrl);

                if (posts.Meta is null)
                {
                    throw new InvalidApiResponseException($"{nameof(SubRedditReadResponse)} contains no meta");
                }

                foreach (RedditPostMeta redditPostMeta in posts.Meta.Children)
                {
                    if (redditPostMeta.RedditPost is null)
                    {
                        throw new InvalidApiResponseException("Post meta contains no data");
                    }

                    yield return redditPostMeta.RedditPost;

                    after = redditPostMeta.RedditPost.Id;
                }
            } while (true);
        }

        public async Task<ApiSubReddit> About(string subreddit)
        {
            if (!subreddit.Contains('/'))
            {
                subreddit = $"/r/{subreddit}";
            }

            if (!subreddit.StartsWith('/'))
            {
                subreddit = $"/{subreddit}";
            }

            SubRedditAboutResponse response = await _jsonClient.Get<SubRedditAboutResponse>($"{API_ROOT}{subreddit}/about");

            return response.Data;
        }

        public async Task SetUpvoteState(ApiThing thing, UpvoteState state)
        {
            await this.EnsureAuthenticated();

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

            string url = $"{API_ROOT}/api/vote?dir={stateInt}&id={thing.Name}";

            await _jsonClient.Post(url);
        }

        public async Task ToggleInboxReplies(ApiThing thing, bool enabled)
        {
            await this.EnsureAuthenticated();

            string url = $"{API_ROOT}/api/sendreplies";

            // Prepare the form values as a dictionary
            Dictionary<string, string> formValues = new()
            {
                { "id", thing.Name },
                { "state", $"{enabled}" }
            };

            await _jsonClient.Post(url, formValues);
        }

        private static string FixSort(string? sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
            {
                sort = "hot";
            }

            if (sort.Length > 0 && sort[0] != '/')
            {
                sort = $"/{sort}";
            }

            sort = sort.ToLower();

            return sort;
        }

        private static void SetParent(ApiThing parent, RedditCommentMeta comment)
        {
            if (comment.Data is null)
            {
                return;
            }

            comment.Data.Parent = parent;

            if (comment.Data.Replies?.Data is null)
            {
                return;
            }

            if (comment.Kind != ThingKind.Comment)
            {
                return;
            }

            foreach (RedditCommentMeta child in comment.Data.Replies.Data.Children)
            {
                SetParent(comment.Data, child);
            }
        }

        private async Task EnsureAuthenticated()
        {
            if (_oAuthToken is null)
            {
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
                HttpResponseMessage response = await _httpClient.PostAsync($"{AUTHORIZATION_ROOT}/api/v1/access_token", content);

                response.EnsureSuccessStatusCode();

                // Read and deserialize the response content
                string responseContent = await response.Content.ReadAsStringAsync();

                _oAuthToken = JsonSerializer.Deserialize<OAuthToken>(responseContent)!;
                _jsonClient.SetDefaultHeader("Authorization", _oAuthToken.TokenType + " " + _oAuthToken.AccessToken);
                LoggedInUser = _redditCredentials.UserName;
            }
        }
    }
}