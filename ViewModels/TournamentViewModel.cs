using MatchBetting.NifsModels;

namespace MatchBetting.ViewModels
{
    public class TournamentViewModel
    {
        public string gruppenamn { get; set; }
        public int id { get; set; }
        public int yearStart { get; set; }

        public int StageTypeId { get; set; } // Viktig! Seier om det er gruppespel eller sluttspel

        public TournamentViewModel(TournamentModel.Root grupper)
        {
            gruppenamn = grupper.groupName;
            id = grupper.id;
            yearStart = grupper.yearStart;
            StageTypeId = grupper.stageTypeId;
        }
    }
}
