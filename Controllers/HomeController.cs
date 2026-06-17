using System.Security.Claims;
using MatchBetting.Data;
using MatchBetting.Models;
using MatchBetting.NifsModels;
using MatchBetting.Service;
using MatchBetting.Utils;
using MatchBetting.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Result = MatchBetting.NifsModels.Result;

namespace MatchBetting.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogService _logService;
    private readonly INifsApiService _nifsApiService;

    // NIFS tournament id for VM 2026.
    private const string TournamentId = "56";

    // Første kamp startar 11.06.2026 kl. 21:00 norsk tid.
    // Sidebets blir låst og offentlege 2 timar før første kamp.
    private static readonly DateTime SideBetDeadline = new(2026, 6, 11, 19, 0, 0);
    private static readonly DateTime TournamentStart = new(2026, 6, 11, 21, 0, 0);

    // Brukarar som kan sjå sidebets før fristen.
    private static readonly HashSet<string> SideBetAdminUserIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "8f477990-e3d8-41e4-b67e-5f3185034ec8"
    };

    public HomeController(
        ApplicationDbContext context,
        ILogService logservice,
        INifsApiService nifsApiService)
    {
        _context = context;
        _logService = logservice;
        _nifsApiService = nifsApiService;
    }

    #region Pages

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var matchViewModelList = new List<NifsKampViewModel>();

        try
        {
            var tournamentViewModelList = await _nifsApiService.GetTournamentInfo(TournamentId);

            foreach (var tournamentViewModel in tournamentViewModelList)
            {
                var matchModels = await _nifsApiService.GetKampInfo(tournamentViewModel.id);

                foreach (var match in matchModels)
                {
                    matchViewModelList.Add(new NifsKampViewModel(match, tournamentViewModel));
                    AddOrUpdateMatchInDatabase(match);
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (NifsApiException ex)
        {
            _logService.LogInfo(
                GetCurrentUserId() ?? string.Empty,
                $"NIFS-feil ved lasting av Home/Index: {ex.Message}");

            ViewBag.NifsWarning =
                "Klarte ikkje hente oppdaterte kampdata frå NIFS akkurat no. Viser sist lagra kampar frå databasen.";

            matchViewModelList = GetMatchesFromDatabaseForIndex();
        }
        catch (Exception ex)
        {
            _logService.LogInfo(
                GetCurrentUserId() ?? string.Empty,
                $"Uventa feil ved lasting av Home/Index: {ex.Message}");

            ViewBag.NifsWarning =
                "Noko gjekk gale ved henting av kampdata. Viser sist lagra kampar frå databasen dersom dei finst.";

            matchViewModelList = GetMatchesFromDatabaseForIndex();
        }

        return View(matchViewModelList);
    }

    [Authorize]
    public async Task<IActionResult> LeaderBoard()
    {
        var currentUserId = GetCurrentUserId();
        var currentUser = await GetCurrentUser(currentUserId);
        var groupId = currentUser?.CompetitionGroupCompetitionId;

        if (groupId == null) return Forbid();

        var matchesToRefresh = GetMatchesWithinTimeRange();

        foreach (var match in matchesToRefresh)
            try
            {
                var fetchedMatch = await _nifsApiService.FetchMatch(match.MatchId);
                AddOrUpdateMatchInDatabase(fetchedMatch);
            }
            catch (Exception ex)
            {
                _logService.LogInfo(
                    currentUserId ?? string.Empty,
                    $"Klarte ikkje oppdatere kamp {match.MatchId} frå NIFS ved leaderboard: {ex.Message}");
            }

        await _context.SaveChangesAsync();

        var currentMatches = GetMatchesWithinTimeRange();
        var users = GetLeaderboardUsersFast(groupId.Value, currentMatches);

        ViewBag.CurrentMatches = currentMatches;

        return View(users);
    }

    [Authorize]
    public async Task<IActionResult> Historikk()
    {
        var currentUserId = GetCurrentUserId();
        var currentUser = await GetCurrentUser(currentUserId);
        var groupId = currentUser?.CompetitionGroupCompetitionId;

        if (groupId == null) return Forbid();

        await RefreshRecentMatchesFromNifs(currentUserId);

        var historicalMatches = GetAllMatchesUpToTimeRange();
        var users = GetLeaderboardUsersFast(groupId.Value, historicalMatches);

        ViewBag.CurrentMatches = historicalMatches;

        return View(users);
    }

    private async Task RefreshRecentMatchesFromNifs(string? userId)
    {
        var now = GetServerDateTimeNow();

        var matchesToRefresh = _context.Matches
            .Where(m =>
                m.Timestamp >= TournamentStart &&
                now >= m.Timestamp.AddHours(-2) &&
                (
                    m.MatchStatusId != 1 ||
                    m.Timestamp >= now.AddDays(-2)
                ))
            .OrderBy(m => m.Timestamp)
            .ToList();

        foreach (var match in matchesToRefresh)
            try
            {
                var fetchedMatch = await _nifsApiService.FetchMatch(match.MatchId);
                AddOrUpdateMatchInDatabase(fetchedMatch);
            }
            catch (Exception ex)
            {
                _logService.LogInfo(
                    userId ?? string.Empty,
                    $"Klarte ikkje oppdatere kamp {match.MatchId} frå NIFS: {ex.Message}");
            }

        await _context.SaveChangesAsync();
    }

    [Authorize]
    public async Task<IActionResult> SideBets()
    {
        var currentUserId = GetCurrentUserId();

        ViewBag.SideBetDeadline = SideBetDeadline;
        ViewBag.IsAdminPreview = IsSideBetAdmin(currentUserId) && !IsSideBetLocked();

        if (!AreSideBetsVisible(currentUserId)) return View(new List<AllSideBettingsViewModel>());

        var currentUser = await GetCurrentUser(currentUserId);
        var groupId = currentUser?.CompetitionGroupCompetitionId;

        if (groupId == null) return Forbid();

        var users = _context.Set<ApplicationUser>()
            .Where(u => u.CompetitionGroupCompetitionId == groupId)
            .ToList()
            .Select(user => new AllSideBettingsViewModel
            {
                UserId = user.Id,
                UserName = user.FullName,
                CurrentSideBettings = GetAllUsersSideBettings(user.Id)
            })
            .OrderBy(u => u.UserName)
            .ToList();

        return View(users);
    }

    [Authorize]
    public IActionResult Rules()
    {
        return View();
    }

    #endregion

    #region Betting endpoints

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateStorage(int matchId, string result)
    {
        var userId = GetCurrentUserId();
        var now = GetServerDateTimeNow();

        try
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new Exception("User is not authenticated");

            if (!new[] { "H", "U", "B" }.Contains(result)) throw new Exception("Invalid betting result");

            var match = _context.Matches.FirstOrDefault(m => m.MatchId == matchId);
            if (match == null || now.AddHours(2) > match.Timestamp)
                throw new Exception("Cannot bet on this match when starting time is less than two hours from now");

            var dbMatchBetting = _context.MatchBettings
                .FirstOrDefault(m => m.UserId == userId && m.MatchId == matchId);

            if (dbMatchBetting == null)
            {
                _context.MatchBettings.Add(new Models.MatchBetting
                {
                    MatchId = matchId,
                    Result = result,
                    UserId = userId
                });
            }
            else
            {
                dbMatchBetting.Result = result;
                _context.MatchBettings.Update(dbMatchBetting);
            }

            await _context.SaveChangesAsync();

            var message = $"Successfully stored match id {matchId} with result {result} for user {userId}";
            _logService.LogInfo(userId, message);

            return Json(new { Success = true, Message = message });
        }
        catch (Exception ex)
        {
            var message = $"Failed to store match id {matchId} with result {result}. Error: {ex.Message}";
            _logService.LogInfo(userId ?? string.Empty, message);

            return Json(new { Success = false, Message = message });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> RemoveStorage(int matchId)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId)) throw new Exception("User is not authenticated");

            var matchBetting = _context.MatchBettings
                .FirstOrDefault(m => m.UserId == userId && m.MatchId == matchId);

            if (matchBetting == null)
                return Json(new
                {
                    Success = true,
                    Message = $"No betting found for match id {matchId} for user {userId}"
                });

            _context.MatchBettings.Remove(matchBetting);
            await _context.SaveChangesAsync();

            return Json(new
            {
                Success = true,
                Message = $"Successfully removed match id {matchId} for user {userId}"
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                Success = false,
                Message = $"Failed to remove match id {matchId}. Error: {ex.Message}"
            });
        }
    }

    [Authorize]
    public IActionResult GetCurrentUserBettings()
    {
        try
        {
            var userId = GetCurrentUserId();

            var bettings = _context.MatchBettings
                .Where(m => m.UserId == userId)
                .Select(mb => new MatchBettingViewModel(mb))
                .ToList();

            return Json(new { Success = true, Bettings = bettings });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                Success = false,
                Message = $"Failed to get bettings. Error: {ex.Message}"
            });
        }
    }

    #endregion

    #region Sidebet endpoints

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateSideBets(SideBettingMinViewModel sideBet)
    {
        try
        {
            if (IsSideBetLocked()) throw new Exception("Sidebets are locked");

            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(userId)) throw new Exception("User is not authenticated");

            sideBet.Toppscorer = sideBet.Toppscorer?.Trim() ?? string.Empty;
            sideBet.WinnerTeam = sideBet.WinnerTeam?.Trim() ?? string.Empty;
            sideBet.MostCards = sideBet.MostCards?.Trim() ?? string.Empty;

            var dbSideBet = _context.SideBettings.FirstOrDefault(m => m.UserId == userId);

            if (dbSideBet == null)
            {
                _context.SideBettings.Add(new SideBet
                {
                    Toppscorer = sideBet.Toppscorer,
                    WinnerTeam = sideBet.WinnerTeam,
                    MostCards = sideBet.MostCards,
                    UserId = userId
                });
            }
            else
            {
                dbSideBet.Toppscorer = sideBet.Toppscorer;
                dbSideBet.WinnerTeam = sideBet.WinnerTeam;
                dbSideBet.MostCards = sideBet.MostCards;

                _context.SideBettings.Update(dbSideBet);
            }

            await _context.SaveChangesAsync();

            return Json(new { Success = true, Message = "Sidebets lagra" });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                Success = false,
                Message = $"Failed to store sidebettings. Error: {ex.Message}"
            });
        }
    }

    [Authorize]
    public IActionResult GetCurrentUserSideBettings()
    {
        try
        {
            var userId = GetCurrentUserId();

            var sideBettings = _context.SideBettings.FirstOrDefault(m => m.UserId == userId)
                               ?? new SideBet
                               {
                                   UserId = userId ?? string.Empty,
                                   Toppscorer = string.Empty,
                                   WinnerTeam = string.Empty,
                                   MostCards = string.Empty
                               };

            return Json(new
            {
                Success = true,
                SideBettings = new SideBettingViewModel(sideBettings)
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                Success = false,
                Message = $"Failed to get sidebettings. Error: {ex.Message}"
            });
        }
    }

    #endregion

    #region Search endpoints

    [Authorize]
    public IActionResult SearchPlayers(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return Json(new List<string>());

        q = q.Trim();

        var results = _context.FootballPlayers
            .Where(p => EF.Functions.Like(p.Name, $"%{q}%"))
            .Select(p => new
            {
                p.Name,
                Score =
                    EF.Functions.Like(p.Name, $"{q}%") ? 0 :
                    EF.Functions.Like(p.Name, $"% {q}%") ? 1 :
                    2
            })
            .OrderBy(x => x.Score)
            .ThenBy(x => x.Name)
            .Take(10)
            .Select(x => x.Name)
            .ToList();

        return Json(results);
    }

    [Authorize]
    public IActionResult SearchTeams(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return Json(new List<string>());

        q = q.Trim();

        var teamNames = _context.Matches
            .Select(m => m.HomeTeam)
            .Concat(_context.Matches.Select(m => m.AwayTeam))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        var results = teamNames
            .Where(name => name.Contains(q, StringComparison.CurrentCultureIgnoreCase))
            .Select(name => new
            {
                Name = name,
                Score =
                    name.StartsWith(q, StringComparison.CurrentCultureIgnoreCase) ? 0 :
                    name.Contains($" {q}", StringComparison.CurrentCultureIgnoreCase) ? 1 :
                    2
            })
            .OrderBy(x => x.Score)
            .ThenBy(x => x.Name)
            .Take(10)
            .Select(x => x.Name)
            .ToList();

        return Json(results);
    }

    #endregion

    #region Match endpoints

    [Authorize]
    public async Task<IActionResult> FetchMatch(int matchId)
    {
        try
        {
            var match = await _nifsApiService.FetchMatch(matchId);
            return Json(match);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Private user helpers

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task<ApplicationUser?> GetCurrentUser(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        return await _context.Set<ApplicationUser>().FindAsync(userId);
    }

    private List<LeaderBoardByUserViewModel> GetLeaderboardUsers(
        int groupId,
        List<MatchViewModel> matches)
    {
        return _context.Set<ApplicationUser>()
            .Where(u => u.CompetitionGroupCompetitionId == groupId)
            .ToList()
            .Select(user => new LeaderBoardByUserViewModel
            {
                UserId = user.Id,
                UserName = user.FullName,
                Score = CalculatePoints(user.Id),
                CurrentBettings = GetCurrentUserCurrentBettings(user.Id, matches)
            })
            .OrderByDescending(u => u.Score)
            .ThenBy(u => u.UserName)
            .ToList();
    }

    // for å unngå N+1-spørringar på leaderboard/historikk.
    private List<LeaderBoardByUserViewModel> GetLeaderboardUsersFast(
    int groupId,
    List<MatchViewModel> matches)
    {
        var now = GetServerDateTimeNow();
        var matchIds = matches.Select(m => m.MatchId).ToHashSet();

        var users = _context.Set<ApplicationUser>()
            .AsNoTracking()
            .Where(u => u.CompetitionGroupCompetitionId == groupId)
            .Select(u => new
            {
                u.Id,
                u.FullName
            })
            .ToList();

        var userIds = users.Select(u => u.Id).ToHashSet();

        var allBettings = _context.MatchBettings
            .AsNoTracking()
            .Where(b => userIds.Contains(b.UserId))
            .Select(b => new
            {
                b.Id,
                b.UserId,
                b.MatchId,
                b.Result
            })
            .ToList();

        var resultMatches = _context.Matches
            .AsNoTracking()
            .Where(m =>
                m.Timestamp >= TournamentStart &&
                m.Result != string.Empty &&
                now >= m.Timestamp)
            .Select(m => new
            {
                m.MatchId,
                m.Result
            })
            .ToList();

        var resultByMatchId = resultMatches
            .ToDictionary(m => m.MatchId, m => m.Result);

        var bettingsByUserAndMatch = allBettings
            .GroupBy(b => b.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(b => b.MatchId, b => b));

        return users
            .Select(user =>
            {
                bettingsByUserAndMatch.TryGetValue(user.Id, out var userBettings);

                var currentBettings = matches
                    .Select(match =>
                    {
                        if (userBettings != null &&
                            userBettings.TryGetValue(match.MatchId, out var betting))
                        {
                            return new MatchBettingViewModel
                            {
                                Id = betting.Id,
                                MatchId = betting.MatchId,
                                Result = betting.Result,
                                UserId = betting.UserId
                            };
                        }

                        return new MatchBettingViewModel(match, user.Id);
                    })
                    .ToList();

                var score = allBettings
                    .Where(b => b.UserId == user.Id)
                    .Count(b =>
                        resultByMatchId.TryGetValue(b.MatchId, out var correctResult) &&
                        b.Result == correctResult);

                return new LeaderBoardByUserViewModel
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    Score = score,
                    CurrentBettings = currentBettings
                };
            })
            .OrderByDescending(u => u.Score)
            .ThenBy(u => u.UserName)
            .ToList();
    }

    #endregion

    #region Private match helpers

    private List<NifsKampViewModel> GetMatchesFromDatabaseForIndex()
    {
        var knockoutStart = new DateTime(2026, 6, 28, 0, 0, 0);

        var matches = _context.Matches
            .Where(m => m.Timestamp >= TournamentStart)
            .OrderBy(m => m.Timestamp)
            .ToList();

        return matches
            .Select(match => new NifsKampViewModel(match, knockoutStart))
            .ToList();
    }

    private List<MatchViewModel> GetMatchesWithinTimeRange()
    {
        var now = GetServerDateTimeNow();

        var matches = _context.Matches
            .Where(m =>
                m.Timestamp >= TournamentStart &&
                now >= m.Timestamp.AddHours(-2) &&
                now <= m.Timestamp.Date.AddDays(1))
            .OrderBy(o => o.Timestamp)
            .ToList();

        return matches.Select(m => new MatchViewModel(m)).ToList();
    }

    private List<MatchViewModel> GetAllMatchesUpToTimeRange()
    {
        var now = GetServerDateTimeNow();

        var matches = _context.Matches
            .Where(m =>
                m.Timestamp >= TournamentStart &&
                now >= m.Timestamp.AddHours(-2))
            .OrderBy(o => o.Timestamp)
            .ToList();

        return matches.Select(m => new MatchViewModel(m)).ToList();
    }

    private void AddOrUpdateMatchInDatabase(NifsKampModel match)
    {
        try
        {
            var dbMatch = _context.Matches.FirstOrDefault(m => m.MatchId == match.id);

            if (dbMatch == null)
            {
                dbMatch = new Match
                {
                    AwayTeam = match.awayTeam.name,
                    HomeTeam = match.homeTeam.name,
                    MatchId = match.id,
                    Timestamp = match.timestamp,
                    HomeScore90 = match.result.homeScore90,
                    AwayScore90 = match.result.awayScore90,
                    Result = GetResultFullTime(match.result),
                    MatchStatusId = match.matchStatusId,
                    MatchStatus = Euro2024MatchStatus.GetMatchStatusText(match.matchStatusId),
                    HomeTeamLogoUrl = match.homeTeam?.logo?.url ?? "~/img/2026_FIFA_World_Cup_emblem.svg",
                    AwayTeamLogoUrl = match.awayTeam?.logo?.url ?? "~/img/2026_FIFA_World_Cup_emblem.svg"
                };

                _context.Matches.Add(dbMatch);
                return;
            }

            dbMatch.AwayTeam = match.awayTeam.name;
            dbMatch.HomeTeam = match.homeTeam.name;
            dbMatch.Timestamp = match.timestamp;
            dbMatch.HomeScore90 = match.result.homeScore90;
            dbMatch.AwayScore90 = match.result.awayScore90;
            dbMatch.Result = GetResultFullTime(match.result);
            dbMatch.MatchStatusId = match.matchStatusId;
            dbMatch.MatchStatus = Euro2024MatchStatus.GetMatchStatusText(match.matchStatusId);
            dbMatch.HomeTeamLogoUrl = match.homeTeam?.logo?.url ?? "~/img/2026_FIFA_World_Cup_emblem.svg";
            dbMatch.AwayTeamLogoUrl = match.awayTeam?.logo?.url ?? "~/img/2026_FIFA_World_Cup_emblem.svg";

            _context.Matches.Update(dbMatch);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add or update match {match.id}: {ex.Message}");
        }
    }

    private static string GetResultFullTime(Result? matchResult)
    {
        if (matchResult?.homeScore90 == null || matchResult.awayScore90 == null) return string.Empty;

        if (matchResult.homeScore90 > matchResult.awayScore90) return "H";

        if (matchResult.homeScore90 < matchResult.awayScore90) return "B";

        return "U";
    }

    #endregion

    #region Private betting helpers

    private List<MatchBettingViewModel> GetCurrentUserCurrentBettings(
        string userId,
        List<MatchViewModel> currentMatches)
    {
        var matchBettings = new List<MatchBettingViewModel>();

        foreach (var match in currentMatches)
        {
            var betting = _context.MatchBettings
                .FirstOrDefault(m => m.UserId == userId && m.MatchId == match.MatchId);

            matchBettings.Add(betting != null
                ? new MatchBettingViewModel(betting)
                : new MatchBettingViewModel(match, userId));
        }

        return matchBettings;
    }

    private int CalculatePoints(string userId)
    {
        var now = GetServerDateTimeNow();

        var bets = _context.MatchBettings
            .Where(mb => mb.UserId == userId)
            .ToList();

        var matchesWithResults = _context.Matches
            .Where(m =>
                m.Timestamp >= TournamentStart &&
                m.Result != string.Empty &&
                now >= m.Timestamp)
            .ToList();

        return matchesWithResults
            .Select(match => bets.FirstOrDefault(b => b.MatchId == match.MatchId))
            .Count(bet =>
                bet != null &&
                bet.Result == matchesWithResults.First(m => m.MatchId == bet.MatchId).Result);
    }

    #endregion

    #region Private sidebet helpers

    private List<SideBettingViewModel> GetAllUsersSideBettings(string userId)
    {
        var sideBettings = _context.SideBettings.FirstOrDefault(m => m.UserId == userId);

        return sideBettings != null
            ? new List<SideBettingViewModel> { new(sideBettings) }
            : new List<SideBettingViewModel>();
    }

    private bool IsSideBetLocked()
    {
        return GetServerDateTimeNow() >= SideBetDeadline;
    }

    private static bool IsSideBetAdmin(string? userId)
    {
        return !string.IsNullOrWhiteSpace(userId) &&
               SideBetAdminUserIds.Contains(userId);
    }

    private bool AreSideBetsVisible(string? userId)
    {
        return IsSideBetLocked() || IsSideBetAdmin(userId);
    }

    #endregion

    #region Private time helpers

    private DateTime GetServerDateTimeNow()
    {
        var osloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, osloTimeZone);
    }

    #endregion
}