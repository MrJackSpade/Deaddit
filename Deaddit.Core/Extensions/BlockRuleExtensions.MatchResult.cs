using static Deaddit.Core.Utils.Blocking.BlockListHelper;

namespace Deaddit.Core.Extensions
{
    public static partial class BlockRuleExtensions
    {
        public class MatchResult
        {
            public TriggerState State { get; private set; } = TriggerState.Skip;

            public bool IsMatch => State == TriggerState.Match;

            public void Apply(TriggerState state)
            {
                switch (state)
                {
                    case TriggerState.Match:
                        if (State == TriggerState.Skip)
                        {
                            State = TriggerState.Match;
                        }

                        break;

                    case TriggerState.Skip:
                        break;

                    case TriggerState.NoMatch:
                        State = TriggerState.NoMatch;
                        break;
                }
            }
        }
    }
}