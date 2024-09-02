namespace Deaddit.Interfaces
{
    public interface IJsonClient
    {
        /// <summary>
        /// Performs a get and returns the deserialized result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<T> Get<T>(string url) where T : class, new();

        /// <summary>
        /// Posts Json and deserializes the result as json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        Task<T> Post<T>(string url, object body) where T : class, new();

        /// <summary>
        /// Posts Json
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        Task Post(string url, object body);

        /// <summary>
        /// Posts a URL encoded form
        /// </summary>
        /// <param name="url"></param>
        /// <param name="formValues"></param>
        /// <returns></returns>
        Task Post(string url, Dictionary<string, string> formValues);

        /// <summary>
        /// Posts a URL encoded form and returns the deserialized response
        /// </summary>
        /// <param name="url"></param>
        /// <param name="formValues"></param>
        /// <returns></returns>
        Task<T> Post<T>(string url, Dictionary<string, string> formValues) where T : class, new();

        /// <summary>
        /// Performs a post with an empty body
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task Post(string url);

        /// <summary>
        /// Sets or updates the value of a header to be used with every subsequent request
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetDefaultHeader(string key, string value);
    }
}