namespace MonkeyType.Shared.DTOs.Requests.StatisticsGame
{
    public class StatisticsGameRequestDTO
    {
        public Guid UserId { get; set; }
        public decimal WordsPerMinute { get; set; }
        public decimal RawWordsPerMinute { get; set; }
        public decimal Accuracy { get; set; }
        public decimal Consistency { get; set; }
        public int CorrectCharacters { get; set; }
        public int IncorrectCharacters { get; set; }
        public int ExtraCharacters { get; set; }
        public int MissedCharacters { get; set; }
        public int DurationInSeconds { get; set; }
        public string Mode { get; set; } = string.Empty;
    }
}