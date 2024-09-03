using Deaddit.Services.DeepCopy.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Services.DeepCopy
{
    internal class ColorCopier : AbstractCopier<Color>
    {
        public override Color Copy(Color inCopy)
        {
            return Color.Parse(inCopy.ToArgbHex());
        }

        public override Color Fill(Color source, Color destination)
        {
            return Color.Parse(source.ToArgbHex());

        }
    }
}
