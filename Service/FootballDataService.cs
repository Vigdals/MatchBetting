using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MatchBetting.Service;

public interface IFootballDataService
{
    Task<List<FootballDataTopScorer>> GetTopScorersAsync(
        string competitionCode,
        int season,
        int limit = 100);
}

public class FootballDataService : IFootballDataService
{
    private readonly HttpClient _httpClient;

    public FootballDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<FootballDataTopScorer>> GetTopScorersAsync(
        string competitionCode,
        int season,
        int limit = 100)
    {
        var url = $"competitions/{competitionCode}/scorers?season={season}&limit={limit}";

        var response = await _httpClient.GetFromJsonAsync<FootballDataScorersResponse>(url);

        if (response?.Scorers == null || response.Scorers.Count == 0)
        {
            return [];
        }

        var maxGoals = response.Scorers.Max(s => s.Goals);

        return response.Scorers
            .Where(s => s.Goals == maxGoals)
            .Select(s => new FootballDataTopScorer
            {
                PlayerName = s.Player.Name,
                TeamName = s.Team.Name,
                Goals = s.Goals,
                Assists = s.Assists,
                Penalties = s.Penalties
            })
            .OrderBy(s => s.PlayerName)
            .ToList();
    }
}

public class FootballDataTopScorer
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int Goals { get; set; }
    public int? Assists { get; set; }
    public int? Penalties { get; set; }
}

public class FootballDataScorersResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("scorers")]
    public List<FootballDataScorer> Scorers { get; set; } = [];
}

public class FootballDataScorer
{
    [JsonPropertyName("player")]
    public FootballDataPlayer Player { get; set; } = new();

    [JsonPropertyName("team")]
    public FootballDataTeam Team { get; set; } = new();

    [JsonPropertyName("playedMatches")]
    public int PlayedMatches { get; set; }

    [JsonPropertyName("goals")]
    public int Goals { get; set; }

    [JsonPropertyName("assists")]
    public int? Assists { get; set; }

    [JsonPropertyName("penalties")]
    public int? Penalties { get; set; }
}

public class FootballDataPlayer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class FootballDataTeam
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = string.Empty;
}