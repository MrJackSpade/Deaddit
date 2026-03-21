namespace Deaddit.Interfaces
{
    public interface IClaudeService
    {
        bool IsConfigured { get; }

        Task<string> SendMessageAsync(string prompt, string input, string? model = null, string? prefill = null, double temperature = 0);
    }
}