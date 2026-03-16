namespace Deaddit.Core.Interfaces
{
    public interface IStreamConverter
    {
        bool CanConvert(string fileName);

        string ConvertFileName(string fileName);

        Task<Stream> ConvertAsync(Stream input);
    }
}
