namespace Deaddit.Configurations
{
    using System.ComponentModel.DataAnnotations;

    public class BlockRule
    {
        public override string? ToString()
        {
            return RuleName;
        }

        [Display(Name = "Author", Order = 3)]
        public string? Author { get; set; }

        [Display(Name = "Block Type", Order = 1)]
        public BlockType BlockType { get; set; }

        [Display(Name = "Body", Order = 7)]
        public string? Body { get; set; }

        [Display(Name = "Flair", Order = 8)]
        public string? Flair { get; set; }

        [Display(Name = "Archived", Order = 4)]
        public bool IsArchived { get; set; }

        [Display(Name = "Locked", Order = 5)]
        public bool IsLocked { get; set; }

        [Display(Name = "Rule Name", Order = 0)]
        public string? RuleName { get; set; }

        [Display(Name = "SubReddit", Order = 2)]
        public string? SubReddit { get; set; }

        [Display(Name = "Title", Order = 6)]
        public string? Title { get; set; }
    }
}