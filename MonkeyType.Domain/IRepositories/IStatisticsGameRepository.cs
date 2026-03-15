using MonkeyType.Domain.Entities;
using MonkeyType.Domain.Models;

namespace MonkeyType.Domain.IRepositories
{
    public interface IStatisticsGameRepository
    {
        Task<IEnumerable<StatisticsGame>?> GetAllAsync();
        Task AddAsync(StatisticsGame statisticsGame);
        Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId);
        Task<StatisticsGame?> GetByIdAsync(Guid id);
        Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(DateTime? startUtc, DateTime? endUtc, int? durationInSeconds, string? mode, int topN);
    }
}