using Keyless.Domain.Entities;
using Keyless.Domain.Models;

namespace Keyless.Domain.IRepositories
{
    public interface IStatisticsGameRepository
    {
        Task<IEnumerable<StatisticsGame>?> GetAllAsync();
        Task<PagedResult<StatisticsGame>> GetPagedAsync(int pageNumber, int pageSize);
        Task AddAsync(StatisticsGame statisticsGame);
        Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId);
        Task<PagedResult<StatisticsGame>> GetByUserIdPagedAsync(Guid userId, int pageNumber, int pageSize);
        Task<StatisticsGame?> GetByIdAsync(Guid id);
        Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(DateTime? startUtc, DateTime? endUtc, int? durationInSeconds, string? mode, int topN);
    }
}