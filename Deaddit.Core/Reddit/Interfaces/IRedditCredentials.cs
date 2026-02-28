namespace Deaddit.Core.Reddit.Interfaces
{
    public interface IRedditCredentials
    {
        bool Valid { get; }
        string? UserName { get; }
        string? Password { get; }
        string? AppKey { get; }
        string? AppSecret { get; }
    }
}
