using Deaddit.Extensions;
using Deaddit.Interfaces;
using Deaddit.Models;
using Deaddit.Models.Json;
using Deaddit.Models.Json.Response;
using System.Text;
using System.Text.Json;

namespace Deaddit.Services
{
    internal class RedditClient : IRedditClient
    {
        private const string API_ROOT = "https://oauth.reddit.com";

        private const string AUTHORIZATION_ROOT = "https://www.reddit.com";

        private readonly HttpClient _httpClient;

        private readonly IJsonClient _jsonClient;

        private readonly string _key;

        private readonly string _password;

        private readonly string _secret;

        private readonly string _username;

        private OAuthToken _oAuthToken;

        public RedditClient(IAppCredentials appCredentials, IJsonClient jsonClient, HttpClient httpClient)
        {
            _key = appCredentials.AppKey;
            _httpClient = httpClient;
            _secret = appCredentials.AppSecret;
            _username = appCredentials.UserName;
            _password = appCredentials.Password;
            _jsonClient = jsonClient;
            _jsonClient.SetDefaultHeader("User-Agent", "Deaddit");
        }

        public async Task<RedditCommentMeta> Comment(RedditThing thing, string comment)
        {
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
                throw new NullReferenceException("API returned an invalid response");
            }

            if (response.Json.Errors.Count > 0)
            {
                List<Exception> exceptions = new();

                foreach (var error in response.Json.Errors)
                {
                    exceptions.Add(new Exception(error));
                }

                throw new AggregateException(exceptions.ToArray());
            }

            return response.Json.Data.Things.Single();
        }

        public async Task<List<CommentReadResponse>> Comments(RedditThing parent)
        {
            string fullUrl = $"{API_ROOT}/comments/{parent.Id}";

            List<CommentReadResponse> response = await _jsonClient.Get<List<CommentReadResponse>>(fullUrl);

            foreach (CommentReadResponse commentReadResponse in response)
            {
                if (commentReadResponse.Data?.Children != null)
                {
                    foreach (RedditCommentMeta child in commentReadResponse.Data.Children)
                    {
                        this.SetParent(parent, child);
                    }
                }
            }

            return response;
        }

        public async Task<Stream> GetStream(string url)
        {
            return await _httpClient.GetStreamAsync(url);
        }

        public async IAsyncEnumerable<RedditPost> Read(string subreddit, string? sort = null, string? after = null, RedditRegion region = RedditRegion.GLOBAL)
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

                foreach (RedditPostMeta redditPostMeta in posts.Meta.Children)
                {
                    yield return redditPostMeta.RedditPost;

                    after = redditPostMeta.RedditPost.Id;
                }
            } while (true);
        }

        public async Task SetUpvoteState(RedditThing thing, UpvoteState state)
        {
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

        private async Task EnsureAuthenticated()
        {
            if (_oAuthToken is null)
            {
                string text = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(_key + ":" + _secret));

                // Set the Authorization header
                _httpClient.SetDefaultHeader("Authorization", "Basic " + text);

                // Encode the form values
                string encodedUsername = Uri.EscapeDataString(_username);
                string encodedPassword = Uri.EscapeDataString(_password);

                // Prepare the content with encoded values
                StringContent content = new($"grant_type=password&username={encodedUsername}&password={encodedPassword}",
                    System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

                // Make the POST request
                HttpResponseMessage response = await _httpClient.PostAsync($"{AUTHORIZATION_ROOT}/api/v1/access_token", content);

                response.EnsureSuccessStatusCode();

                // Read and deserialize the response content
                string responseContent = await response.Content.ReadAsStringAsync();

                _oAuthToken = JsonSerializer.Deserialize<OAuthToken>(responseContent)!;
                _jsonClient.SetDefaultHeader("Authorization", _oAuthToken.TokenType + " " + _oAuthToken.AccessToken);
            }
        }


        private void SetParent(RedditThing parent, RedditCommentMeta comment)
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
                this.SetParent(comment.Data, child);
            }
        }
    }
}