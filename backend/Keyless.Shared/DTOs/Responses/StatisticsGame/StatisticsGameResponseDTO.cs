using System.ComponentModel.DataAnnotations;

namespace Keyless.Shared.DTOs.Responses.StatisticsGame
{
    public class StatisticsGameResponseDTO
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal WordsPerMinute { get; set; }

        [Range(0, double.MaxValue)]
        public decimal RawWordsPerMinute { get; set; }

        [Range(0, 100)]
        public decimal Accuracy { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Consistency { get; set; }

        [Range(0, int.MaxValue)]
        public int CorrectCharacters { get; set; }

        [Range(0, int.MaxValue)]
        public int IncorrectCharacters { get; set; }

        [Range(0, int.MaxValue)]
        public int ExtraCharacters { get; set; }

        [Range(0, int.MaxValue)]
        public int MissedCharacters { get; set; }

        [Range(1, int.MaxValue)]
        public int DurationInSeconds { get; set; }

        [StringLength(50, MinimumLength = 1)]
        public string Mode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
