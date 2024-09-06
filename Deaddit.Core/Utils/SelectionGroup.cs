using Deaddit.Core.Interfaces;

namespace Deaddit.Core.Utils
{
    public class SelectionGroup
    {
        private ISelectionGroupItem? _selected = null;

        public bool IsSelected(ISelectionGroupItem selected)
        {
            return _selected == selected;
        }

        public void Select(ISelectionGroupItem selected)
        {
            if (!selected.SelectEnabled)
            {
                return;
            }

            _selected?.Unselect();
            _selected = selected;
            selected.Select();
        }

        public void Toggle(ISelectionGroupItem item)
        {
            if (this.IsSelected(item))
            {
                this.Unselect(item);
            }
            else
            {
                this.Select(item);
            }
        }

        public void Unselect(ISelectionGroupItem selected)
        {
            if (this.IsSelected(selected))
            {
                _selected?.Unselect();
                _selected = null;
            }
        }
    }
}