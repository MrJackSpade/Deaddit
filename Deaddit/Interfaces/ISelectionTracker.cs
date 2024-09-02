using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Interfaces
{
    public interface ISelectionTracker
    {
        public bool IsSelected(ISelectable selected);

        public void Select(ISelectable selected);

        public void Unselect(ISelectable selected);
    }
}