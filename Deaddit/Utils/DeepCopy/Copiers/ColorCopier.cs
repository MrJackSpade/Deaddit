namespace Deaddit.Utils.DeepCopy.Copiers
{
    internal class ColorCopier : AbstractCopier<Color>
    {
        public override Color? Copy(Color? inCopy)
        {
            if (inCopy is null)
            {
                return null;
            }

            return Color.Parse(inCopy.ToArgbHex());
        }

        public override Color? Fill(Color? source, Color? destination)
        {
            if (source is null)
            {
                return null;
            }

            return Color.Parse(source.ToArgbHex());

        }
    }
}
