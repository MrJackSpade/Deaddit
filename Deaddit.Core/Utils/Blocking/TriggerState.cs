namespace Deaddit.Core.Utils.Blocking
{

    public static partial class BlockListHelper
    {
        public enum TriggerState
        {
            Invalid,
            Match,
            NoMatch,
            Skip
        }
    }
}