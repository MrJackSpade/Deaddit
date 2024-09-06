namespace Deaddit.Core.Utils.DeepCopy.Copiers
{
    public class StringCopier : AbstractCopier<string>
    {
        public override string? Copy(string? inCopy)
        {
            return inCopy;
        }

        public override string? Fill(string? source, string? destination)
        {
            return source;
        }
    }
}