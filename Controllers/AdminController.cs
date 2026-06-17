using System.Security.Claims;
using MatchBetting.Data;
using MatchBetting.Models;
using MatchBetting.Service;
using MatchBetting.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MatchBetting.NifsModels;
using MatchBetting.Utils;
using Result = MatchBetting.NifsModels.Result;

namespace MatchBetting.Controllers;

[Authorize]
public class AdminController : Controller
{
    private const string TournamentId = "56";
    private static readonly DateTime TournamentStart = new(2026, 6, 11, 21, 0, 0);

    private static readonly HashSet<string> AllowedAdminUserIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "8f477990-e3d8-41e4-b67e-5f3185034ec8"
    };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INifsApiService _nifsApiService;
    private readonly IServiceProvider _serviceProvider;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        INifsApiService nifsApiService,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _userManager = userManager;
        _nifsApiService = nifsApiService;
        _serviceProvider = serviceProvider;
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
        var sideBetResult = await _context.SideBetResults
            .FirstOrDefaultAsync(r => r.TournamentId == TournamentId);

        var users = await _context.Users
            .Include(u => u.CompetitionGroup)
            .Where(u => u.Email != null && u.Email.Trim() != string.Empty)
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
            GroupCount = groups.Count,

            SideBetResultToppscorer = sideBetResult?.Toppscorer ?? string.Empty,
            SideBetResultWinnerTeam = sideBetResult?.WinnerTeam ?? string.Empty,
            SideBetResultMostCards = sideBetResult?.MostCards ?? string.Empty,
            SideBetResultUpdatedAtUtc = sideBetResult?.UpdatedAtUtc
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSideBetResult(string? toppscorer, string? winnerTeam, string? mostCards)
    {
        toppscorer = toppscorer?.Trim() ?? string.Empty;
        winnerTeam = winnerTeam?.Trim() ?? string.Empty;
        mostCards = mostCards?.Trim() ?? string.Empty;

        var sideBetResult = await _context.SideBetResults
            .FirstOrDefaultAsync(r => r.TournamentId == TournamentId);

        if (sideBetResult == null)
        {
            sideBetResult = new SideBetResult
            {
                TournamentId = TournamentId
            };

            _context.SideBetResults.Add(sideBetResult);
        }

        sideBetResult.Toppscorer = toppscorer;
        sideBetResult.WinnerTeam = winnerTeam;
        sideBetResult.MostCards = mostCards;
        sideBetResult.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = "Sidebets er lagra.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FetchTopScorerFromFootballData()
    {
        const string competitionCode = "WC";
        const int season = 2026;

        List<FootballDataTopScorer> topScorers;

        try
        {
            var footballDataService = _serviceProvider.GetRequiredService<IFootballDataService>();

            topScorers = await footballDataService
                .GetTopScorersAsync(competitionCode, season);
        }
        catch (Exception ex)
        {
            TempData["AdminMessage"] =
                $"Klarte ikkje hente toppscorar frå Football-Data akkurat no: {ex.Message}";

            return RedirectToAction(nameof(Index));
        }

        if (topScorers.Count == 0)
        {
            TempData["AdminMessage"] =
                "Fann ingen toppscorarar frå Football-Data. Turneringa er truleg ikkje starta, eller API-et har ikkje data enno.";

            return RedirectToAction(nameof(Index));
        }

        var sideBetResult = await _context.SideBetResults
            .FirstOrDefaultAsync(r => r.TournamentId == TournamentId);

        if (sideBetResult == null)
        {
            sideBetResult = new SideBetResult
            {
                TournamentId = TournamentId
            };

            _context.SideBetResults.Add(sideBetResult);
        }

        sideBetResult.Toppscorer = string.Join(", ", topScorers.Select(s => s.PlayerName));
        sideBetResult.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var scorerText = string.Join(
            ", ",
            topScorers.Select(s => $"{s.PlayerName} ({s.Goals})"));

        TempData["AdminMessage"] = $"Oppdaterte toppscorar frå Football-Data: {scorerText}.";

        return RedirectToAction(nameof(Index));
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

        foreach (var user in users) user.CompetitionGroupCompetitionId = null;

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
    public async Task<IActionResult> ResetPassword(string userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["AdminMessage"] = "Manglar brukar-id.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["AdminMessage"] = "Nytt passord manglar.";
            return RedirectToAction(nameof(Index));
        }

        if (newPassword.Length < 4)
        {
            TempData["AdminMessage"] = "Passordet må vere minst 4 teikn.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["AdminMessage"] = "Fann ikkje brukaren.";
            return RedirectToAction(nameof(Index));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            TempData["AdminMessage"] =
                $"Klarte ikkje å resette passord for {DisplayName(user)}: " +
                string.Join(", ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }

        TempData["AdminMessage"] = $"Passordet er oppdatert for {DisplayName(user)}.";
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
                changed++;
        }

        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"Normaliserte {changed} sidebet-rader.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncMatchesFromNifs()
    {
        var now = GetServerDateTimeNow();

        var matches = await _context.Matches
            .Where(m =>
                m.Timestamp >= TournamentStart &&
                now >= m.Timestamp.AddHours(-2) &&
                (
                    m.MatchStatusId != 1 ||
                    m.Timestamp >= now.AddDays(-2)
                ))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        var updated = 0;
        var failed = 0;
        var changed = 0;

        foreach (var dbMatch in matches)
        {
            try
            {
                var beforeStatusId = dbMatch.MatchStatusId;
                var beforeHomeScore = dbMatch.HomeScore90;
                var beforeAwayScore = dbMatch.AwayScore90;
                var beforeResult = dbMatch.Result;

                var fetchedMatch = await _nifsApiService.FetchMatch(dbMatch.MatchId);

                ApplyNifsMatch(dbMatch, fetchedMatch);

                updated++;

                if (beforeStatusId != dbMatch.MatchStatusId ||
                    beforeHomeScore != dbMatch.HomeScore90 ||
                    beforeAwayScore != dbMatch.AwayScore90 ||
                    beforeResult != dbMatch.Result)
                {
                    changed++;
                }
            }
            catch (Exception ex)
            {
                failed++;

                Console.WriteLine($"Klarte ikkje synke kamp {dbMatch.MatchId} frå NIFS: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();

        TempData["AdminMessage"] =
            $"Synka kampar frå NIFS. Sjekka: {matches.Count}. Oppdatert: {updated}. Endra: {changed}. Feila: {failed}.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedPlayers()
    {
        var apiNames = await _nifsApiService.GetAllPlayersForTournament(TournamentId);

        var normalizedApiNames = apiNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var apiNameSet = normalizedApiNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dbPlayers = await _context.FootballPlayers.ToListAsync();

        var added = 0;
        var removed = 0;
        var renamed = 0;

        foreach (var dbPlayer in dbPlayers)
        {
            var trimmedName = dbPlayer.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                _context.FootballPlayers.Remove(dbPlayer);
                removed++;
                continue;
            }

            if (!string.Equals(dbPlayer.Name, trimmedName, StringComparison.Ordinal))
            {
                dbPlayer.Name = trimmedName;
                renamed++;
            }

            if (!apiNameSet.Contains(trimmedName))
            {
                _context.FootballPlayers.Remove(dbPlayer);
                removed++;
            }
        }

        var dbNameSet = dbPlayers
            .Where(p => !_context.Entry(p).State.Equals(EntityState.Deleted))
            .Select(p => p.Name?.Trim() ?? string.Empty)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in normalizedApiNames)
        {
            if (dbNameSet.Contains(name)) continue;

            _context.FootballPlayers.Add(new FootballPlayers
            {
                Name = name
            });

            added++;
        }

        await _context.SaveChangesAsync();

        TempData["AdminMessage"] =
            $"Synka spelarar frå NIFS. Nye: {added}. Sletta: {removed}. Trimma: {renamed}. Totalt frå NIFS: {normalizedApiNames.Count}.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPlayer(string? name)
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

    private static void ApplyNifsMatch(Match dbMatch, NifsKampModel match)
    {
        dbMatch.AwayTeam = match.awayTeam.name;
        dbMatch.HomeTeam = match.homeTeam.name;
        dbMatch.Timestamp = match.timestamp;
        dbMatch.HomeScore90 = match.result?.homeScore90;
        dbMatch.AwayScore90 = match.result?.awayScore90;
        dbMatch.Result = GetResultFullTime(match.result);
        dbMatch.MatchStatusId = match.matchStatusId;
        dbMatch.MatchStatus = Euro2024MatchStatus.GetMatchStatusText(match.matchStatusId);
        dbMatch.HomeTeamLogoUrl = match.homeTeam?.logo?.url ?? "~/img/2026_FIFA_World_Cup_emblem.svg";
        dbMatch.AwayTeamLogoUrl = match.awayTeam?.logo?.url ?? "~/img/2026_FIFA_World_Cup_emblem.svg";
    }

    private static string GetResultFullTime(Result? matchResult)
    {
        if (matchResult?.homeScore90 == null || matchResult.awayScore90 == null)
        {
            return string.Empty;
        }

        if (matchResult.homeScore90 > matchResult.awayScore90)
        {
            return "H";
        }

        if (matchResult.homeScore90 < matchResult.awayScore90)
        {
            return "B";
        }

        return "U";
    }

    private static DateTime GetServerDateTimeNow()
    {
        var osloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, osloTimeZone);
    }

    private static string DisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.FullName)) return user.FullName;
        if (!string.IsNullOrWhiteSpace(user.Email)) return user.Email;
        if (!string.IsNullOrWhiteSpace(user.UserName)) return user.UserName;
        return user.Id;
    }
}