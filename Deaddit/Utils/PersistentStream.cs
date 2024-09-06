using System.Diagnostics;

namespace Deaddit.Utils
{
    internal class PersistentStream : MemoryStream
    {
        public PersistentStream(Stream source)
        {
            source.CopyTo(this);
        }

        public override void Close()
        {
            Debug.WriteLine($"{nameof(PersistentStream)} Intercepted Close() call.");
        }
    }
}
