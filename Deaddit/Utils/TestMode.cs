namespace Deaddit.Utils
{
    /// <summary>
    /// Set by launching the app with DEADDIT_TEST_MODE=1, for driving it from a script on a machine
    /// someone is using: errors go to the log rather than dialogs, and nothing opens a login page.
    /// </summary>
    internal static class TestMode
    {
        public static bool IsEnabled { get; } = Environment.GetEnvironmentVariable("DEADDIT_TEST_MODE") == "1";
    }
}
