using Deaddit.Services.DeepCopy.Interfaces;

namespace Deaddit.Services.DeepCopy
{
    internal class StringCopier : AbstractCopier<string>
    {
        public override string Copy(string inCopy)
        {
            return inCopy;
        }

        public override string Fill(string source, string destination)
        {
            return source;
        }
    }
}
