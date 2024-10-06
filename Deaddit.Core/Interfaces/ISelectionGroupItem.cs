namespace Deaddit.Core.Interfaces
{
    public interface ISelectionGroupItem
    {
        bool SelectEnabled { get; }

        Task Select();

        Task Unselect();
    }
}