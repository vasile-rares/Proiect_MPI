using System.ComponentModel.DataAnnotations;

namespace Keyless.Shared.DTOs.Responses.StatisticsGame
{
    public class UserStatsAggregateResponseDTO
    {
        public Guid UserId { get; set; }

        [Range(0, int.MaxValue)]
        public int GamesCount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal HighestWordsPerMinute { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AverageWordsPerMinute { get; set; }

        [Range(0, double.MaxValue)]
        public decimal HighestRawWordsPerMinute { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AverageRawWordsPerMinute { get; set; }

        [Range(0, 100)]
        public decimal HighestAccuracy { get; set; }

        [Range(0, 100)]
        public decimal AverageAccuracy { get; set; }

        [Range(0, double.MaxValue)]
        public decimal HighestConsistency { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AverageConsistency { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
