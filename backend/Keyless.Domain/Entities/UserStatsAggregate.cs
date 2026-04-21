namespace Keyless.Domain.Entities
{
    public class UserStatsAggregate
    {
        public Guid UserId { get; set; }
        public int GamesCount { get; set; }
        public decimal HighestWordsPerMinute { get; set; }
        public decimal AverageWordsPerMinute { get; set; }
        public decimal HighestRawWordsPerMinute { get; set; }
        public decimal AverageRawWordsPerMinute { get; set; }
        public decimal HighestAccuracy { get; set; }
        public decimal AverageAccuracy { get; set; }
        public decimal HighestConsistency { get; set; }
        public decimal AverageConsistency { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
