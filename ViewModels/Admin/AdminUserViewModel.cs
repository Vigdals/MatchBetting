namespace MatchBetting.ViewModels.Admin;

public class AdminUserViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? CompetitionGroupId { get; set; }
    public string? CompetitionGroupName { get; set; }
    public int MatchBetCount { get; set; }
    public bool HasSideBet { get; set; }
}