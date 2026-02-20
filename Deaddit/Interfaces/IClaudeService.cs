
namespace Deaddit.Interfaces
{
    public interface IClaudeService
    {
        Task<string> SendMessageAsync(string prompt, string input, string? prefill = null, double temperature = 0);
    }
}
