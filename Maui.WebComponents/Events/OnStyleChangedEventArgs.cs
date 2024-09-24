using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maui.WebComponents.Events
{
    internal class OnStyleChangedEventArgs : EventArgs
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }
}