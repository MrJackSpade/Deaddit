namespace Deaddit.Utils.DeepCopy.Interfaces
{
    internal interface ITypeCopier
    {
        object? Copy(object? inCopy);

        object? Fill(object? source, object? destination);
    }
}