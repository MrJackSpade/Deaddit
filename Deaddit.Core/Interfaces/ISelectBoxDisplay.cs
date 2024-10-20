namespace Deaddit.Core.Interfaces
{
    public interface ISelectBoxDisplay
    {
        Task<string> Select(string? title, string?[] options, string? defaultOption = null);
    }
}