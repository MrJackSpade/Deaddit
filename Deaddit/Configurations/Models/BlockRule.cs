using Deaddit.MAUI.Attributes;

namespace Deaddit.Configurations.Models
{
    public class BlockRule
    {
        [EditorDisplay(Name = "Author", Order = 3)]
        public string? Author { get; set; }

        [EditorDisplay(Name = "Block Type", Order = 1)]
        public BlockType BlockType { get; set; }

        [EditorDisplay(Name = "Body", Order = 7)]
        public string? Body { get; set; }

        [EditorDisplay(Name = "Domain", Order = 7)]
        public string? Domain { get; set; }

        [EditorDisplay(Name = "Flair", Order = 8)]
        public string? Flair { get; set; }

        [EditorDisplay(Name = "Archived", Order = 4)]
        public bool IsArchived { get; set; }

        [EditorDisplay(Name = "Locked", Order = 5)]
        public bool IsLocked { get; set; }

        [EditorDisplay(Name = "Rule Name", Order = 0)]
        public string? RuleName { get; set; }

        [EditorDisplay(Name = "SubReddit", Order = 2)]
        public string? SubReddit { get; set; }

        [EditorDisplay(Name = "Title", Order = 6)]
        public string? Title { get; set; }

        public override string? ToString()
        {
            return RuleName;
        }
    }
}