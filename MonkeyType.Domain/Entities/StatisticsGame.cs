namespace MonkeyType.Domain.Entities
{
    public class StatisticsGame
    {
        public Guid Id { get; set; }
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
        public string Mode { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
    }
}