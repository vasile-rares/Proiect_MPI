using MonkeyType.Domain.Entities;
using MonkeyType.Shared.DTOs.Requests.StatisticsGame;
using MonkeyType.Shared.DTOs.Responses.StatisticsGame;
using MonkeyType.Domain.Models;

namespace MonkeyType.Application.IServices
{
    public interface IStatisticsGameService
    {
        Task<IEnumerable<StatisticsGameResponseDTO>?> GetAllAsync();
        Task<PagedResult<StatisticsGameResponseDTO>> GetPagedAsync(int pageNumber, int pageSize);
        Task AddAsync(StatisticsGameRequestDTO statisticsGame);
        Task<IEnumerable<StatisticsGameResponseDTO>?> GetByUserIdAsync(Guid userId);
        Task<PagedResult<StatisticsGameResponseDTO>> GetByUserIdPagedAsync(Guid userId, int pageNumber, int pageSize);
        Task<StatisticsGameResponseDTO?> GetByIdAsync(Guid id);
        Task<UserStatsAggregate?> GetAggregateByUserIdAsync(Guid userId);
        Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(string scope, int? durationInSeconds, string? mode, int topN);
    }
}