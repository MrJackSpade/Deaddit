using Deaddit.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Services
{
    internal class SelectionTracker : ISelectionTracker
    {
        private ISelectable? _selected = null;

        public bool IsSelected(ISelectable selected)
        {
            return _selected == selected;
        }

        public void Select(ISelectable selected)
        {
            if (!selected.SelectEnabled)
            {
                return;
            }

            _selected?.Unselect();
            _selected = selected;
            selected.Select();
        }

        public void Unselect(ISelectable selected)
        {
            if (this.IsSelected(selected))
            {
                _selected?.Unselect();
                _selected = null;
            }
        }
    }
}