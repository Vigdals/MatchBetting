namespace MatchBetting.NifsModels
{
    public class PlayerModel
    {
        public string name { get; set; }
        public List<Player> players { get; set; }

        public class Player
        {
            public string name { get; set; }
            public string birthDate { get; set; }
            public int? shirtNumber { get; set; }
            public Position? position { get; set; }
            public int id { get; set; }
        }

        public class Position
        {
            public string position { get; set; }
            public int id { get; set; }
        }
    }
}