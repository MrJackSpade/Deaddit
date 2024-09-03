using Deaddit.Utils.DeepCopy.Interfaces;

namespace Deaddit.Utils.DeepCopy.Copiers
{
    public abstract class AbstractCopier<T> : ITypeCopier where T : class
    {
        public abstract T? Copy(T? source);

        object? ITypeCopier.Copy(object? inCopy)
        {
            return this.Copy(inCopy as T);
        }

        public abstract T? Fill(T? source, T? destination);

        object? ITypeCopier.Fill(object? source, object? destination)
        {
            return this.Fill(source as T, destination as T);
        }
    }
}