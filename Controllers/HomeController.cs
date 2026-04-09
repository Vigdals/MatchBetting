using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MatchBetting.NifsModels;
using MatchBetting.Utils;
using MatchBetting.ViewModels;
using System.Security.Claims;
using MatchBetting.Data;
using MatchBetting.Models;
using MatchBetting.Service;
using Result = MatchBetting.NifsModels.Result;

namespace MatchBetting.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogService _logService;
        private readonly INifsApiService _nifsApiService;

        private readonly string TournamentID = "56";
        //private readonly string TournamentID = "59";

        public HomeController(ApplicationDbContext context, ILogService logservice, INifsApiService nifsApiService)
        {
            _context = context;
            _logService = logservice;
            _nifsApiService = nifsApiService;
        }

        #region ControllerActions

        [Authorize]
        public IActionResult Index()
        {
            var TournamentViewModelList = _nifsApiService.GetTournamentInfo(TournamentID);
            var matchViewModelList = new List<NifsKampViewModel>();

            foreach (var tournamentViewModel in TournamentViewModelList)
            {
                var matchModels = _nifsApiService.GetKampInfo(tournamentViewModel.id);
                foreach (var match in matchModels.Result)
                {
                    matchViewModelList.Add(new NifsKampViewModel(match, tournamentViewModel));
                    AddOrUpdateMatchInDatabase(match);
                    Debug.WriteLine(tournamentViewModel.gruppenamn + match.homeTeam.name + " + " + match.awayTeam.name);
                }
            }

            try { _context.SaveChangesAsync(); }
            catch (Exception e) { Console.WriteLine(e); }

            return View(matchViewModelList);
        }

        [Authorize]
        public IActionResult Rules() => View();

        [Authorize]
        public async Task<IActionResult> LeaderBoard()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Set<ApplicationUser>().FindAsync(currentUserId);
            var gruppeId = currentUser?.CompetitionGroupCompetitionId;

            var currentMatches = GetMatchesWithinTimeRange();

            foreach (var match in currentMatches)
            {
                var thematch = await _nifsApiService.FetchMatch(match.MatchId);
                AddOrUpdateMatchInDatabase(thematch);
            }

            var users = _context.Set<ApplicationUser>()
                .Where(u => u.CompetitionGroupCompetitionId == gruppeId)
                .ToList()
                .Select(user => new LeaderBoardByUserViewModel
                {
                    UserId = user.Id,
                    //UserName = Euro2024Users.HentBrukernavn(user.Id),
                    UserName = user.FullName,
                    Score = CalculatePoints(user.Id),
                    CurrentBettings = GetCurrentUserCurrentBettings(user.Id, currentMatches)
                }).ToList();

            ViewBag.CurrentMatches = currentMatches;

            try { await _context.SaveChangesAsync(); }
            catch (Exception e) { Console.WriteLine(e); }

            return View(users);
        }

        [Authorize]
        public async Task<IActionResult> Historikk()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Set<ApplicationUser>().FindAsync(currentUserId);
            var gruppeId = currentUser?.CompetitionGroupCompetitionId;

            var currentMatches = GetAllMatchesUpToTimeRange();

            var users = _context.Set<ApplicationUser>()
                .Where(u => u.CompetitionGroupCompetitionId == gruppeId)
                .ToList()
                .Select(user => new LeaderBoardByUserViewModel
                {
                    UserId = user.Id,
                    //UserName = Euro2024Users.HentEtternavn(user.Id),
                    UserName = user.FullName,
                    Score = CalculatePoints(user.Id),
                    CurrentBettings = GetCurrentUserCurrentBettings(user.Id, currentMatches)
                }).ToList();

            ViewBag.CurrentMatches = currentMatches;

            return View(users);
        }

        [Authorize]
        public async Task<IActionResult> SideBets()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.Set<ApplicationUser>().FindAsync(currentUserId);
            var gruppeId = currentUser?.CompetitionGroupCompetitionId;

            var users = _context.Set<ApplicationUser>()
                .Where(u => u.CompetitionGroupCompetitionId == gruppeId)
                .ToList()
                .Select(user => new AllSideBettingsViewModel
                {
                    UserId = user.Id,
                    //UserName = Euro2024Users.HentBrukernavn(user.Id),
                    UserName = user.FullName,
                    CurrentSideBettings = GetAllUsersSideBettings(user.Id)
                }).ToList();

            return View(users);
        }

        #endregion

        #region HelperMethods

        private List<MatchViewModel> GetAllMatchesUpToTimeRange()
        {
            var now = GetServerDateTimeNow();
            var matches = _context.Matches
                .Where(m => now >= m.Timestamp.AddHours(-2))
                .OrderBy(o => o.Timestamp)
                .ToList();
            return matches.Select(m => new MatchViewModel(m)).ToList();
        }

        private List<MatchViewModel> GetMatchesWithinTimeRange()
        {
            var now = GetServerDateTimeNow();
            var matches = _context.Matches
                .Where(m => now >= m.Timestamp.AddHours(-2) && now <= m.Timestamp.Date.AddDays(1))
                .OrderBy(o => o.Timestamp)
                .ToList();
            return matches.Select(m => new MatchViewModel(m)).ToList();
        }

        private List<MatchBettingViewModel> GetCurrentUserCurrentBettings(string userId, List<MatchViewModel> currentMatches)
        {
            var matchBettings = new List<MatchBettingViewModel>();
            foreach (var match in currentMatches)
            {
                var betting = _context.MatchBettings.FirstOrDefault(m => m.UserId == userId && m.MatchId == match.MatchId);
                matchBettings.Add(betting != null
                    ? new MatchBettingViewModel(betting)
                    : new MatchBettingViewModel(match, userId));
            }
            return matchBettings;
        }

        private int CalculatePoints(string userId)
        {
            var now = GetServerDateTimeNow();
            var bets = _context.MatchBettings.Where(mb => mb.UserId == userId).ToList();
            var matchesWithResults = _context.Matches.Where(m => m.Result != string.Empty && now >= m.Timestamp).ToList();

            return matchesWithResults
                .Select(match => bets.FirstOrDefault(b => b.MatchId == match.MatchId))
                .Count(bet => bet != null && bet.Result == matchesWithResults
                    .First(m => m.MatchId == bet.MatchId).Result);
        }

        private DateTime GetServerDateTimeNow()
        {
            var osloTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, osloTimeZone);
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
                }
                else
                {
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
            }
            catch (Exception ex) { }
        }

        private string GetResultFullTime(Result matchResult)
        {
            if (matchResult.homeScore90 > matchResult.awayScore90) return "H";
            if (matchResult.homeScore90 < matchResult.awayScore90) return "B";
            return "U";
        }

        #endregion

        #region Apis

        [HttpPost]
        public async Task<IActionResult> UpdateStorage(int matchId, string result)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var match = _context.Matches.FirstOrDefault(m => m.MatchId == matchId);
                if (match == null || DateTime.Now.AddHours(2) > match.Timestamp)
                    throw new Exception("Cannot bet on this match when starting time is less than two hours from now");

                var dbMatchBetting = _context.MatchBettings.FirstOrDefault(m => m.UserId == userId && m.MatchId == matchId);
                if (dbMatchBetting != null) await RemoveStorage(dbMatchBetting.MatchId);

                _context.MatchBettings.Add(new Models.MatchBetting { MatchId = matchId, Result = result, UserId = userId });
                await _context.SaveChangesAsync();

                var message = $"Successfully stored match id {matchId} with result {result} for user {userId}";
                _logService.LogInfo(userId, message);
                return Json(new { Success = true, Message = message });
            }
            catch (Exception ex)
            {
                var message = $"Failed to store match id {matchId} with result {result}. Error: {ex.Message}";
                _logService.LogInfo(userId, message);
                return Json(new { Success = false, Message = message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSideBets(SideBettingMinViewModel sideBet)
        {
            sideBet.MostCards ??= string.Empty;
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var sidebet = _context.SideBettings.FirstOrDefault(m => m.UserId == userId);
                if (sidebet == null)
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
                    sidebet.Toppscorer = sideBet.Toppscorer;
                    sidebet.WinnerTeam = sideBet.WinnerTeam;
                    sidebet.MostCards = sideBet.MostCards;
                    _context.SideBettings.Update(sidebet);
                }
                await _context.SaveChangesAsync();
                return Json(new { Success = true, Message = $"Successfully stored sidebettings for user {User.FindFirstValue(ClaimTypes.NameIdentifier)}" });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = $"Failed to store sidebettings. Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveStorage(int matchId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var matchBetting = _context.MatchBettings.FirstOrDefault(m => m.UserId == userId && m.MatchId == matchId);
                _context.MatchBettings.Remove(matchBetting);
                await _context.SaveChangesAsync();
                return Json(new { Success = true, Message = $"Successfully removed match id {matchId} for user {userId}" });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = $"Failed to remove match id {matchId}. Error: {ex.Message}" });
            }
        }

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

        public IActionResult GetCurrentUserBettings()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var bettings = _context.MatchBettings.Where(m => m.UserId == userId).Select(mb => new MatchBettingViewModel(mb));
                return Json(new { Success = true, Bettings = bettings });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = $"Failed to get bettings. Error: {ex.Message}" });
            }
        }

        public IActionResult GetCurrentUserSideBettings()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var sideBettings = _context.SideBettings.FirstOrDefault(m => m.UserId == userId)
                    ?? new SideBet { UserId = userId };
                return Json(new { Success = true, SideBettings = new SideBettingViewModel(sideBettings) });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = $"Failed to get sidebettings. Error: {ex.Message}" });
            }
        }

        public List<SideBettingViewModel> GetAllUsersSideBettings(string userId)
        {
            try
            {
                var sideBettings = _context.SideBettings.FirstOrDefault(m => m.UserId == userId);
                return sideBettings != null
                    ? new List<SideBettingViewModel> { new SideBettingViewModel(sideBettings) }
                    : new List<SideBettingViewModel>();
            }
            catch { return new List<SideBettingViewModel>(); }
        }

        public IActionResult GetGoalsAndCardsforPlayer(string playerId)
        {
            return View();
        }

        #endregion
    }
}