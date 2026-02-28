namespace Deaddit.Core.Reddit.Interfaces
{
    public interface IRedditCredentials
    {
        string? AppKey { get; }

        string? AppSecret { get; }

        string? Password { get; }

        string? UserName { get; }

        bool Valid { get; }
    }
}