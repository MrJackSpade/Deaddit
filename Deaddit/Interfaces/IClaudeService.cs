
namespace Deaddit.Interfaces
{
    public interface IClaudeService
    {
        Task<string> SendMessageAsync(string prompt, string input, string? model = null, string? prefill = null, double temperature = 0);
    }
}
