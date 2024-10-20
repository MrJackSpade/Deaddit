using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Core.Reddit.Exceptions
{
    public class DisplayException : Exception
    {
        public DisplayException(string message) : base(message) { }
    }
}
