namespace GamingPlatform.Domain.DTO
{
    public class StudioRefDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // The public page of a studio
    public class StudioPageDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<GameDto> Games { get; set; } = new();
        public int ReviewCount { get; set; }
        public string ReviewSummary { get; set; } = string.Empty;   // across all its games
        public int? PositivePercent { get; set; }
        public bool CanViewDashboard { get; set; }
    }

    // The Dev dashboard: how a studio's games are doing
    public class StudioDashboardDto
    {
        public Guid StudioId { get; set; }
        public string StudioName { get; set; } = string.Empty;

        public decimal Revenue { get; set; }      // paid orders, refunds left out
        public int UnitsSold { get; set; }        // paid copies, gifts included
        public int Owners { get; set; }           // everyone who has a game in their library (free ones too)
        public int ReviewCount { get; set; }
        public double? AverageRating { get; set; }

        public List<GameStatsDto> Games { get; set; } = new();
        public List<MonthlyRatingDto> RatingByMonth { get; set; } = new(); // the last 12 months, oldest first

        public List<StudioRefDto> AllStudios { get; set; } = new(); // admins can switch between studios
    }

    public class GameStatsDto
    {
        public Guid GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsDelisted { get; set; }
        public int Owners { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public int ReviewCount { get; set; }
        public double? AverageRating { get; set; }
        public string ReviewSummary { get; set; } = string.Empty;
    }

    public class MonthlyRatingDto
    {
        public DateTime Month { get; set; }       // the first day of the month
        public int ReviewCount { get; set; }
        public double? AverageRating { get; set; }
    }
}
