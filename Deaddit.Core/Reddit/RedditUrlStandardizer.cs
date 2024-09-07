namespace Deaddit.Core.Reddit
{
    internal class RedditUrlStandardizer(string username)
    {
        private readonly string _userName = username;

        public string Standardize(string url)
        {
            if (url.StartsWith("/m/"))
            {
                url = $"/user/me{url}";
            }

            if (url.StartsWith("/u/"))
            {
                url = "/user/" + url[3..];
            }

            if (url.StartsWith("u/"))
            {
                url = "/user/" + url[2..];
            }

            //Weird hack but this is how the website works too so
            //I dont feel bad about it.
            if (url.StartsWith("/user/me/"))
            {
                url = $"/user/{_userName}/" + url[9..];
            }

            return url;
        }
    }
}