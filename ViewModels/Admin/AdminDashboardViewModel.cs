using MatchBetting.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MatchBetting.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int UserCount { get; set; }
    public int UsersWithoutGroupCount { get; set; }
    public int MatchCount { get; set; }
    public int MatchBetCount { get; set; }
    public int SideBetCount { get; set; }
    public int PlayerCount { get; set; }
    public int GroupCount { get; set; }

    public string SideBetResultToppscorer { get; set; } = string.Empty;
    public string SideBetResultWinnerTeam { get; set; } = string.Empty;
    public string SideBetResultMostCards { get; set; } = string.Empty;
    public DateTime? SideBetResultUpdatedAtUtc { get; set; }

    public List<AdminUserViewModel> Users { get; set; } = new();
    public List<SelectListItem> Groups { get; set; } = new();
}