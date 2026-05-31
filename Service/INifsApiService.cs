using System.Text.Json;
using MatchBetting.NifsModels;
using MatchBetting.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace MatchBetting.Service;

public class NifsApiService : INifsApiService
{
    private const int TournamentYear = 2026;

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NifsApiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NifsApiService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<NifsApiService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<TournamentViewModel>> GetTournamentInfo(string tournamentId)
    {
        var cacheKey = $"nifs:tournament:{tournamentId}:stages:{TournamentYear}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            var tournamentModels = await GetGruppeInfo($"tournaments/{tournamentId}/stages/");

            return tournamentModels
                .Where(gruppe => gruppe.yearStart == TournamentYear)
                .Select(gruppe => new TournamentViewModel(gruppe))
                .ToList();
        }) ?? new List<TournamentViewModel>();
    }

    public async Task<List<TournamentModel.Root>> GetGruppeInfo(string apiEndpoint)
    {
        return await GetFromNifsAsync<List<TournamentModel.Root>>(apiEndpoint)
               ?? new List<TournamentModel.Root>();
    }

    public async Task<List<NifsKampModel>> GetKampInfo(int tournamentId)
    {
        var cacheKey = $"nifs:stage:{tournamentId}:matches";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            return await GetFromNifsAsync<List<NifsKampModel>>($"stages/{tournamentId}/matches/")
                   ?? new List<NifsKampModel>();
        }) ?? new List<NifsKampModel>();
    }

    public async Task<NifsKampModel> FetchMatch(int matchId)
    {
        var match = await GetFromNifsAsync<NifsKampModel>($"matches/{matchId}/?summary=1");

        if (match == null)
        {
            throw new InvalidOperationException($"NIFS returnerte tom respons for kamp {matchId}.");
        }

        return match;
    }

    public async Task<List<string>> GetAllPlayersForTournament(string tournamentId)
    {
        var cacheKey = $"nifs:tournament:{tournamentId}:players:{TournamentYear}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

            var stages = await GetTournamentInfo(tournamentId);
            var teamIds = new HashSet<int>();
            var playerNames = new List<string>();

            foreach (var stage in stages)
            {
                var matches = await GetKampInfo(stage.id);

                foreach (var match in matches)
                {
                    if (match.homeTeam?.id > 0)
                    {
                        teamIds.Add(match.homeTeam.id);
                    }

                    if (match.awayTeam?.id > 0)
                    {
                        teamIds.Add(match.awayTeam.id);
                    }
                }
            }

            foreach (var teamId in teamIds)
            {
                try
                {
                    var team = await FetchPlayer(teamId);

                    var names = team?.players?
                        .Select(p => p.name?.Trim())
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Select(n => n!)
                        .ToList();

                    if (names != null)
                    {
                        playerNames.AddRange(names);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Klarte ikkje hente spelarar for NIFS-lag {TeamId}.", teamId);
                }
            }

            return playerNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();
        }) ?? new List<string>();
    }

    public async Task<PlayerModel> FetchPlayer(int teamId)
    {
        var playerModel = await GetFromNifsAsync<PlayerModel>($"teams/{teamId}/");

        if (playerModel == null)
        {
            throw new InvalidOperationException($"NIFS returnerte tom respons for lag {teamId}.");
        }

        return playerModel;
    }

    private async Task<T?> GetFromNifsAsync<T>(string relativeUrl)
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();

            var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);

            if (result == null)
            {
                _logger.LogWarning("NIFS returnerte tom eller ugyldig JSON for {RelativeUrl}.", relativeUrl);
            }

            return result;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout ved kall mot NIFS: {RelativeUrl}.", relativeUrl);
            throw new NifsApiException("NIFS brukte for lang tid på å svare.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP-feil ved kall mot NIFS: {RelativeUrl}.", relativeUrl);
            throw new NifsApiException("Klarte ikkje hente data frå NIFS.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Klarte ikkje lese JSON frå NIFS: {RelativeUrl}.", relativeUrl);
            throw new NifsApiException("NIFS returnerte data i eit format appen ikkje klarte å lese.", ex);
        }
    }
}

public interface INifsApiService
{
    Task<List<TournamentViewModel>> GetTournamentInfo(string tournamentId);
    Task<List<TournamentModel.Root>> GetGruppeInfo(string apiEndpoint);
    Task<List<NifsKampModel>> GetKampInfo(int tournamentId);
    Task<NifsKampModel> FetchMatch(int matchId);
    Task<PlayerModel> FetchPlayer(int teamId);
    Task<List<string>> GetAllPlayersForTournament(string tournamentId);
}

public class NifsApiException : Exception
{
    public NifsApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}