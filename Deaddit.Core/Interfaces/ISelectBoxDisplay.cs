using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Core.Interfaces
{
    public interface ISelectBoxDisplay
    {
        Task<string> Select(string? title, string?[] options, string? defaultOption = null);
    }
}