using System.Text.Json.Serialization;

namespace MatchBetting.NifsModels
{
    public class NifsKampModel
    {
        [JsonConverter(typeof(DateTimeOffsetToDateTimeConverter))]
        public DateTime timestamp { get; set; }

        public string name { get; set; }
        public Result result { get; set; }
        public Team homeTeam { get; set; }
        public Team awayTeam { get; set; }
        public int matchStatusId { get; set; }
        public int? matchTypeId { get; set; }
        public Stadium stadium { get; set; }
        public int? attendance { get; set; }
        public int round { get; set; }
        public string comment { get; set; }
        public List<int> tv2Ids { get; set; }
        public bool coveredLive { get; set; }
        public int stageId { get; set; }
        public List<MatchStream> matchStreams { get; set; }
        public List<TvChannel> tvChannels { get; set; }

        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime lastUpdated { get; set; }

        public string matchLength { get; set; }
        public int? surfaceId { get; set; }
        public string type { get; set; }
        public string uid { get; set; }
        public int id { get; set; }
        public ExternalIds externalIds { get; set; }
        public int sportId { get; set; }
    }

    public class Result
    {
        public int? homeScore90 { get; set; }
        public int? awayScore90 { get; set; }
        public string type { get; set; }
        public string uid { get; set; }
    }

    public class Team
    {
        public Image logo { get; set; }
        public object matchStatistics { get; set; }
        public string name { get; set; }
        public Image teamPhoto { get; set; }
        public object names { get; set; }
        public List<Club> clubs { get; set; }
        public object teamInStageStatusId { get; set; }
        public string type { get; set; }
        public string uid { get; set; }
        public int id { get; set; }
        public ExternalIds externalIds { get; set; }
    }

    public class Image
    {
        public string url { get; set; }
        public int imageTypeId { get; set; }
        public string type { get; set; }
        public string uid { get; set; }
        public int? id { get; set; }
    }

    public class Club
    {
        public string name { get; set; }
        public string displayName { get; set; }
        public string primaryColor { get; set; }
        public string secondaryColor { get; set; }
        public string textColor { get; set; }
        public string address { get; set; }
        public string homePage { get; set; }

        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? dateFounded { get; set; }

        public string type { get; set; }
        public string uid { get; set; }
        public int id { get; set; }
        public int sportId { get; set; }
    }

    public class Stadium
    {
        public string? name { get; set; }
        public string? type { get; set; }
        public string? uid { get; set; }
        public int? id { get; set; }
        public int? sportId { get; set; }
    }

    public class MatchStream
    {
        public string link { get; set; }
        public Customer customer { get; set; }
        public bool radio { get; set; }
        public string type { get; set; }
        public string uid { get; set; }
        public int? id { get; set; }
        public object externalIds { get; set; }
        public int sportId { get; set; }
    }

    public class Customer
    {
        public string name { get; set; }
        public int pubId { get; set; }
        public int customerConcernId { get; set; }
        public bool prioritizeLocalTeamsInFriendlies { get; set; }
        public string host { get; set; }
        public string type { get; set; }
        public string uid { get; set; }
        public int id { get; set; }
        public object externalIds { get; set; }
    }

    public class TvChannel
    {
        public string name { get; set; }
        public object progId { get; set; }
        public string type { get; set; }
        public string uid { get; set; }
        public int id { get; set; }
        public object externalIds { get; set; }
        public int sportId { get; set; }
    }

    public class ExternalIds
    {
        public List<int> tv2 { get; set; }
        public List<int> fiks { get; set; }
    }
}
