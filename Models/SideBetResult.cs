using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MatchBetting.Models;

public class SideBetResult
{
    [Key] public int Id { get; set; }
    public string TournamentId { get; set; } = string.Empty;
    public string Toppscorer { get; set; } = string.Empty;
    public string WinnerTeam { get; set; } = string.Empty;
    public string MostCards { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}