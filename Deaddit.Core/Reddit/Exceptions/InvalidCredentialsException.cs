using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Core.Reddit.Exceptions
{
    public class InvalidCredentialsException : DisplayException
    {
        public InvalidCredentialsException() : base("You must be logged in to perform this action")
        {
        }
    }
}
