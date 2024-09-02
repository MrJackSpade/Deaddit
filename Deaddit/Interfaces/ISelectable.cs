﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Interfaces
{
    public interface ISelectable
    {
        bool SelectEnabled { get; }

        void Select();

        void Unselect();
    }
}
