using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maui.WebComponents.Exceptions
{
    public class JavascriptException : Exception
    {
        public JavascriptException(string message) : base(message)
        {
        }
    }
}
