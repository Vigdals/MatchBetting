namespace MatchBetting.Models
{
    public class CompetitionGroup
    {
        public int CompetitionId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool isactive { get; set; } = true;
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}
