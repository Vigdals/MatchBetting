using System.Text.Json;
using MatchBetting.NifsModels;
using MatchBetting.Utils;
using MatchBetting.ViewModels;

namespace MatchBetting.Service;

public class NifsApiService : INifsApiService
{
    public NifsApiService()
    {
    }

    public List<TournamentViewModel> GetTournamentInfo(string tournamentID)
    {
        var tournamentModels = GetGruppeInfo("https://api.nifs.no/tournaments/" + tournamentID + "/stages/");

        var TournamentViewModelList = new List<TournamentViewModel>();

        //adding info just to display to the view
        foreach (var gruppe in tournamentModels.Result)
        {
            if (gruppe.yearStart == 2026)
            {
                TournamentViewModelList.Add(new TournamentViewModel(gruppe));
            }
        }
        return TournamentViewModelList;
    }

    public async Task<List<TournamentModel.Root>> GetGruppeInfo(string apiEndpoint)
    {
        var jsonResult = await ApiCall.DoApiCallAsync(apiEndpoint);

        //The most sexy oneliner in the world!
        //Takes the jsonResult, deserializes it and adds it to my model. Crazy easy
        var tournamentModels = JsonSerializer.Deserialize<List<TournamentModel.Root>>(jsonResult);

        return tournamentModels;
    }

    public async Task<List<NifsKampModel>> GetKampInfo(int tournamentId)
    {
        string apiEndpoint = $"https://api.nifs.no/stages/{tournamentId}/matches/";

        var jsonResult = await ApiCall.DoApiCallAsync(apiEndpoint);

        var matchModel = JsonSerializer.Deserialize<List<NifsKampModel>>(jsonResult);

        return matchModel;
    }

    public async Task<NifsKampModel> FetchMatch(int matchId)
    {
        var apiEndpoint = $"https://api.nifs.no/matches/{matchId}/?summary=1";

        var jsonResult = await ApiCall.DoApiCallAsync(apiEndpoint);

        try
        {
            var matchModel = JsonSerializer.Deserialize<NifsKampModel>(jsonResult);
            return matchModel;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    // gotta iterate through teams, get player then add them to a model
    // https://api.nifs.no/teams/963 for USA
    public async Task<PlayerModel> FetchPlayer(int teamId)
    {
        // iterate through 
        var apiEndpoint = $"https://api.nifs.no/teams/{teamId}/";
        var jsonResult = await ApiCall.DoApiCallAsync(apiEndpoint);
        try
        {
            var playerModel = JsonSerializer.Deserialize<PlayerModel>(jsonResult);
            return playerModel;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}

public interface INifsApiService
{
    List<TournamentViewModel> GetTournamentInfo(string tournamentID);
    Task<List<TournamentModel.Root>> GetGruppeInfo(string apiEndpoint);
    Task<List<NifsKampModel>> GetKampInfo(int tournamentId);
    Task<NifsKampModel> FetchMatch(int matchId);
    Task<PlayerModel> FetchPlayer(int teamId);
}