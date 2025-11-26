using System.ComponentModel.DataAnnotations;

namespace MatchBetting.Models
{
    public class FootballPlayers
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? CountryCode { get; set; }

        [MaxLength(100)]
        public string? ExternalApiId { get; set; }
    }
}
