using Deaddit.Core.Utils.Models;

namespace Deaddit.Core.Utils.DeepCopy.Copiers
{
    public class ColorCopier : AbstractCopier<DynamicColor>
    {
        public override DynamicColor? Copy(DynamicColor? inCopy)
        {
            if (inCopy is null)
            {
                return null;
            }

            return DynamicColor.Parse(inCopy.ToArgbHex());
        }

        public override DynamicColor? Fill(DynamicColor? source, DynamicColor? destination)
        {
            if (source is null)
            {
                return null;
            }

            return DynamicColor.Parse(source.ToArgbHex());
        }
    }
}