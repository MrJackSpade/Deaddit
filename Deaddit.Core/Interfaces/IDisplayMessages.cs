namespace Deaddit.Core.Interfaces
{
    public interface IDisplayMessages
    {
        /// <summary>
        /// Displays the exception. Returns false if the exception should be rethrown.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        Task<bool> DisplayException(Exception exception);

        /// <summary>
        /// Displays a message to the user. Returns false if the message was not shown
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<bool> DisplayMessage(string message);
    }
}