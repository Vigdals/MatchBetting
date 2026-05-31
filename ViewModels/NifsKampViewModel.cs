using MatchBetting.NifsModels;

namespace MatchBetting.ViewModels
{
    public class NifsKampViewModel
    {
        public string homeTeam { get; set; }
        public string awayTeam { get; set; }
        public string result { get; set; }
        public string? stadium { get; set; }
        public string phase { get; set; }
        public int round { get; set; }
        public DateTime date { get; set; }
        public string? gruppe { get; set; }
        public int id { get; set; }
        public string HomeTeamLogoUrl { get; set; }
        public string AwayTeamLogoUrl { get; set; }

        private const string FallbackLogo = "~/img/worldcup-2026.png";

        public NifsKampViewModel(NifsKampModel match, TournamentViewModel model)
        {
            homeTeam = match.homeTeam?.name ?? string.Empty;
            awayTeam = match.awayTeam?.name ?? string.Empty;
            result = $"{match.result?.homeScore90} - {match.result?.awayScore90}";
            stadium = match.stadium?.name;
            round = match.round;
            date = match.timestamp;
            gruppe = model.gruppenamn;
            id = match.id;
            phase = model.StageTypeId == 1 ? "group" : "knockout";
            HomeTeamLogoUrl = match.homeTeam?.logo?.url ?? FallbackLogo;
            AwayTeamLogoUrl = match.awayTeam?.logo?.url ?? FallbackLogo;
        }

        public NifsKampViewModel(NifsKampModel match)
        {
            homeTeam = match.homeTeam?.name ?? string.Empty;
            awayTeam = match.awayTeam?.name ?? string.Empty;
            result = $"{match.result?.homeScore90} - {match.result?.awayScore90}";
            stadium = match.stadium?.name;
            round = match.round;
            date = match.timestamp;
            id = match.id;
            phase = "group";
            HomeTeamLogoUrl = match.homeTeam?.logo?.url ?? FallbackLogo;
            AwayTeamLogoUrl = match.awayTeam?.logo?.url ?? FallbackLogo;
        }

        public NifsKampViewModel(Models.Match match, DateTime knockoutStart)
        {
            homeTeam = match.HomeTeam ?? string.Empty;
            awayTeam = match.AwayTeam ?? string.Empty;
            result = $"{match.HomeScore90} - {match.AwayScore90}";
            stadium = string.Empty;
            round = CalculateRound(match.Timestamp);
            date = match.Timestamp;
            gruppe = string.Empty;
            id = match.MatchId;
            phase = match.Timestamp >= knockoutStart ? "knockout" : "group";
            HomeTeamLogoUrl = string.IsNullOrWhiteSpace(match.HomeTeamLogoUrl)
                ? FallbackLogo
                : match.HomeTeamLogoUrl;
            AwayTeamLogoUrl = string.IsNullOrWhiteSpace(match.AwayTeamLogoUrl)
                ? FallbackLogo
                : match.AwayTeamLogoUrl;
        }

        private static int CalculateRound(DateTime timestamp)
        {
            if (timestamp.Date <= new DateTime(2026, 6, 17))
            {
                return 1;
            }

            if (timestamp.Date <= new DateTime(2026, 6, 24))
            {
                return 2;
            }

            return 3;
        }
    }
}