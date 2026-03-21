using static Deaddit.Core.Utils.Blocking.BlockListHelper;

namespace Deaddit.Core.Extensions
{
    public static partial class BlockRuleExtensions
    {
        /// <summary>
        /// Accumulates the result of matching multiple rule fields against a thing.
        ///
        /// Uses three-state logic (Skip/Match/NoMatch) to implement AND semantics
        /// across only the fields that are actually populated on the rule:
        ///
        ///   - Skip:    The rule field was empty/null, so it doesn't participate in matching.
        ///              A rule with only Flair set won't care about Author, Domain, etc.
        ///   - Match:   The rule field had a value and it matched the thing's value.
        ///   - NoMatch: The rule field had a value and it did NOT match. This is sticky —
        ///              once any field fails to match, the whole rule can never match.
        ///              This enforces AND: "block flair X in subreddit Y" requires BOTH.
        ///
        /// The initial state is Skip (no fields checked yet). The first populated field
        /// that matches promotes the state to Match. Any field that fails kills it permanently.
        /// If all fields were empty (state stays Skip), IsMatch returns false — a rule
        /// with nothing configured blocks nothing.
        /// </summary>
        public class MatchResult
        {
            /// <summary>
            /// True only if at least one field matched and no fields failed.
            /// </summary>
            public bool IsMatch => State == TriggerState.Match;

            public TriggerState State { get; private set; } = TriggerState.Skip;

            /// <summary>
            /// Folds a single field's match result into the accumulated state.
            ///
            /// Truth table:
            ///   Current State | Applied State | Result
            ///   --------------|---------------|--------
            ///   Skip          | Skip          | Skip      (neither field was populated)
            ///   Skip          | Match         | Match     (first populated field matched)
            ///   Skip          | NoMatch       | NoMatch   (first populated field failed)
            ///   Match         | Skip          | Match     (unpopulated field, no effect)
            ///   Match         | Match         | Match     (another field also matched)
            ///   Match         | NoMatch       | NoMatch   (a field failed, kills the rule)
            ///   NoMatch       | Skip          | NoMatch   (already dead, stays dead)
            ///   NoMatch       | Match         | NoMatch   (already dead, stays dead)
            ///   NoMatch       | NoMatch       | NoMatch   (still dead)
            /// </summary>
            public void Apply(TriggerState state)
            {
                switch (state)
                {
                    // Field was populated and matched. Only promote if we haven't
                    // already failed — once NoMatch, nothing can save it.
                    case TriggerState.Match:
                        if (State == TriggerState.Skip)
                        {
                            State = TriggerState.Match;
                        }

                        break;

                    // Field was empty/null — doesn't participate in matching at all.
                    case TriggerState.Skip:
                        break;

                    // Field was populated and did NOT match. Unconditionally kills
                    // the result — this is the AND enforcement.
                    case TriggerState.NoMatch:
                        State = TriggerState.NoMatch;
                        break;
                }
            }
        }
    }
}