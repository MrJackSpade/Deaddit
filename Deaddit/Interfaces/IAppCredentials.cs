namespace Deaddit.Interfaces
{
    public interface IAppCredentials
    {
        string AppKey { get;  }
        string AppSecret { get;  }
        string? Password { get;  }
        string? UserName { get;  }
    }
}
