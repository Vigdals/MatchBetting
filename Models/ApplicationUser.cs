using Microsoft.AspNetCore.Identity;

namespace MatchBetting.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? CompetitionGroupCompetitionId { get; set; }
        public CompetitionGroup? CompetitionGroup { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}