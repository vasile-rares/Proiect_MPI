using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MonkeyType.Shared.DTOs.Requests.StatisticsGame
{
    public class StatisticsGameRequestDTO : IValidatableObject
    {
        [Required]
        public Guid UserId { get; set; }

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

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Mode { get; set; } = string.Empty;

        // Derived on the server to avoid trusting client-calculated metrics.
        [JsonIgnore]
        public decimal WordsPerMinute { get; set; }

        [JsonIgnore]
        public decimal RawWordsPerMinute { get; set; }

        [JsonIgnore]
        public decimal Accuracy { get; set; }

        [JsonIgnore]
        public decimal Consistency { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var totalCharacters = CorrectCharacters + IncorrectCharacters + ExtraCharacters + MissedCharacters;
            if (totalCharacters <= 0)
            {
                yield return new ValidationResult("At least one character count must be provided.", new[]
                {
                    nameof(CorrectCharacters),
                    nameof(IncorrectCharacters),
                    nameof(ExtraCharacters),
                    nameof(MissedCharacters)
                });
            }
        }
    }
}