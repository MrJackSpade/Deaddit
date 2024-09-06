namespace Deaddit.Core.Interfaces
{
    public interface ISelectionGroupItem
    {
        bool SelectEnabled { get; }

        void Select();

        void Unselect();
    }
}