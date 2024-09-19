namespace Deaddit.Core.Interfaces
{
    public interface IDisplayExceptions
    {
        /// <summary>
        /// Displays the exception. Returns false if the exception should be rethrown.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        Task<bool> DisplayException(Exception exception);
    }
}