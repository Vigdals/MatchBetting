using System.ComponentModel.DataAnnotations;

namespace MatchBetting.Models
{
    public class CompetitionGroup
    {
        [Key]
        public int CompetitionId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool isactive { get; set; } = true;
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }

    public class UserGroupMembership
    {
        public string UserId { get; set; } = default!;
        public int CompetitionGroupId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public CompetitionGroup CompetitionGroup { get; set; } = default!;
    }
}
