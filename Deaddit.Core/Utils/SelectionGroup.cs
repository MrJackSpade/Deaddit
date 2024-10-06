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

        public async Task Select(ISelectionGroupItem selected)
        {
            if (!selected.SelectEnabled)
            {
                return;
            }

            _selected?.Unselect();
            _selected = selected;
            await selected.Select();
        }

        public async Task Toggle(ISelectionGroupItem item)
        {
            if (this.IsSelected(item))
            {
                await this.Unselect(item);
            }
            else
            {
                await this.Select(item);
            }
        }

        public async Task Unselect(ISelectionGroupItem selected)
        {
            if (this.IsSelected(selected))
            {
                if (_selected != null)
                {
                    await _selected.Unselect();
                    _selected = null;
                }

                _selected = null;
            }
        }

        public async Task Unselect()
        {
            if(_selected != null)
            {
                await _selected.Unselect();
                _selected = null;
            }
        }
    }
}