using System.Security.Claims;
using MatchBetting.Data;
using MatchBetting.Models;
using MatchBetting.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MatchBetting.Controllers;

[Authorize]
public class AdminController : Controller
{
    private const string TournamentId = "56";

    private static readonly HashSet<string> AllowedAdminUserIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "8f477990-e3d8-41e4-b67e-5f3185034ec8",
        "468d570b-b3f7-4075-8cc4-2681f72a6aec"
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INifsApiService _nifsApiService;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        INifsApiService nifsApiService)
    {
        _context = context;
        _userManager = userManager;
        _nifsApiService = nifsApiService;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) || !AllowedAdminUserIds.Contains(userId))
        {
            context.Result = Forbid();
            return;
        }

        base.OnActionExecuting(context);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .Include(u => u.CompetitionGroup)
            .OrderBy(u => u.CompetitionGroupCompetitionId)
            .ThenBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .Select(u => new AdminUserViewModel
            {
                UserId = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                FullName = u.FullName,
                CompetitionGroupId = u.CompetitionGroupCompetitionId,
                CompetitionGroupName = u.CompetitionGroup != null ? u.CompetitionGroup.Name : null,
                MatchBetCount = _context.MatchBettings.Count(mb => mb.UserId == u.Id),
                HasSideBet = _context.SideBettings.Any(sb => sb.UserId == u.Id)
            })
            .ToListAsync();

        var groups = await _context.CompetitionGroups
            .OrderBy(g => g.Name)
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            Users = users,
            Groups = groups.Select(g => new SelectListItem
            {
                Value = g.CompetitionId.ToString(),
                Text = $"{g.Name}{(g.isactive ? string.Empty : " (inaktiv)")}"
            }).ToList(),
            UserCount = users.Count,
            UsersWithoutGroupCount = users.Count(u => u.CompetitionGroupId == null),
            MatchCount = await _context.Matches.CountAsync(),
            MatchBetCount = await _context.MatchBettings.CountAsync(),
            SideBetCount = await _context.SideBettings.CountAsync(),
            PlayerCount = await _context.FootballPlayers.CountAsync(),
            GroupCount = groups.Count
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUserGroup(string userId, int? competitionGroupId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["AdminMessage"] = "Manglar brukar-id.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["AdminMessage"] = "Fann ikkje brukaren.";
            return RedirectToAction(nameof(Index));
        }

        if (competitionGroupId != null)
        {
            var groupExists = await _context.CompetitionGroups
                .AnyAsync(g => g.CompetitionId == competitionGroupId.Value);

            if (!groupExists)
            {
                TempData["AdminMessage"] = "Fann ikkje gruppa.";
                return RedirectToAction(nameof(Index));
            }
        }

        user.CompetitionGroupCompetitionId = competitionGroupId;
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Oppdaterte gruppe for {DisplayName(user)}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearUsersWithoutEmailGroup()
    {
        var users = await _context.Users
            .Where(u => u.Email == null || u.Email.Trim() == string.Empty)
            .ToListAsync();

        foreach (var user in users)
        {
            user.CompetitionGroupCompetitionId = null;
        }

        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Fjerna gruppe frå {users.Count} brukarar utan e-post.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["AdminMessage"] = "Manglar brukar-id.";
            return RedirectToAction(nameof(Index));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminMessage"] = "Du kan ikkje slette deg sjølv frå adminpanelet.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["AdminMessage"] = "Fann ikkje brukaren.";
            return RedirectToAction(nameof(Index));
        }

        var matchBets = _context.MatchBettings.Where(mb => mb.UserId == userId);
        var sideBets = _context.SideBettings.Where(sb => sb.UserId == userId);

        _context.MatchBettings.RemoveRange(matchBets);
        _context.SideBettings.RemoveRange(sideBets);
        await _context.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(user);

        TempData["AdminMessage"] = result.Succeeded
            ? $"Sletta {DisplayName(user)} og tilhøyrande tips."
            : $"Klarte ikkje å slette {DisplayName(user)}: {string.Join(", ", result.Errors.Select(e => e.Description))}";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FixNullSideBets()
    {
        var sideBets = await _context.SideBettings.ToListAsync();
        var changed = 0;

        foreach (var sideBet in sideBets)
        {
            var oldToppscorer = sideBet.Toppscorer;
            var oldMostCards = sideBet.MostCards;
            var oldWinnerTeam = sideBet.WinnerTeam;

            sideBet.Toppscorer = sideBet.Toppscorer?.Trim() ?? string.Empty;
            sideBet.MostCards = sideBet.MostCards?.Trim() ?? string.Empty;
            sideBet.WinnerTeam = sideBet.WinnerTeam?.Trim() ?? string.Empty;

            if (oldToppscorer != sideBet.Toppscorer ||
                oldMostCards != sideBet.MostCards ||
                oldWinnerTeam != sideBet.WinnerTeam)
            {
                changed++;
            }
        }

        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Normaliserte {changed} sidebet-rader.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedPlayers()
    {
        var names = await _nifsApiService.GetAllPlayersForTournament(TournamentId);
        var added = 0;

        foreach (var name in names
                     .Where(n => !string.IsNullOrWhiteSpace(n))
                     .Select(n => n.Trim())
                     .Distinct())
        {
            if (await _context.FootballPlayers.AnyAsync(p => p.Name == name))
                continue;

            _context.FootballPlayers.Add(new FootballPlayers { Name = name });
            added++;
        }

        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Seeda spelarar. Nye: {added}. Totalt frå NIFS: {names.Count}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPlayer(string name)
    {
        name = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Spelarnamn manglar.";
            return RedirectToAction(nameof(Index));
        }

        if (await _context.FootballPlayers.AnyAsync(p => p.Name == name))
        {
            TempData["AdminMessage"] = $"{name} finst allereie.";
            return RedirectToAction(nameof(Index));
        }

        _context.FootballPlayers.Add(new FootballPlayers { Name = name });
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"La til {name}.";
        return RedirectToAction(nameof(Index));
    }

    private static string DisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.FullName)) return user.FullName;
        if (!string.IsNullOrWhiteSpace(user.Email)) return user.Email;
        if (!string.IsNullOrWhiteSpace(user.UserName)) return user.UserName;
        return user.Id;
    }
}

public class AdminDashboardViewModel
{
    public int UserCount { get; set; }
    public int UsersWithoutGroupCount { get; set; }
    public int MatchCount { get; set; }
    public int MatchBetCount { get; set; }
    public int SideBetCount { get; set; }
    public int PlayerCount { get; set; }
    public int GroupCount { get; set; }

    public List<AdminUserViewModel> Users { get; set; } = new();
    public List<SelectListItem> Groups { get; set; } = new();
}

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