namespace MatchBetting.ViewModels
{
    public class UserViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int Score { get; set; }
        public string CompetitionGroupName { get; set; }

    }

    public class LeaderBoardByUserViewModel : UserViewModel
    {
        public LeaderBoardByUserViewModel()
        {
            CurrentBettings = new List<MatchBettingViewModel>();
        }

        public List<MatchBettingViewModel> CurrentBettings { get; set; }
    }
}