﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Interfaces
{
    public interface IMarkDownService
    {
        string? Clean(string? markdown);
    }
}
