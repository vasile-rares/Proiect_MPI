namespace MonkeyType.Domain.Models
{
    public class LeaderboardEntry
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal WordsPerMinute { get; set; }
        public decimal Accuracy { get; set; }
        public int DurationInSeconds { get; set; }
        public string Mode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
