using Deaddit.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Deaddit.Configurations
{
    public class BlockRule
    {
        public override string? ToString()
        {
            return RuleName;
        }

        [Input(Name = "Author", Order = 3)]
        public string? Author { get; set; }

        [Input(Name = "Block Type", Order = 1)]
        public BlockType BlockType { get; set; }

        [Input(Name = "Body", Order = 7)]
        public string? Body { get; set; }

        [Input(Name = "Domain", Order = 7)]
        public string? Domain { get; set; }

        [Input(Name = "Flair", Order = 8)]
        public string? Flair { get; set; }

        [Input(Name = "Archived", Order = 4)]
        public bool IsArchived { get; set; }

        [Input(Name = "Locked", Order = 5)]
        public bool IsLocked { get; set; }

        [Input(Name = "Rule Name", Order = 0)]
        public string? RuleName { get; set; }

        [Input(Name = "SubReddit", Order = 2)]
        public string? SubReddit { get; set; }

        [Input(Name = "Title", Order = 6)]
        public string? Title { get; set; }
    }
}