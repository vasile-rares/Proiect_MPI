using MonkeyType.Domain.Entities;
using MonkeyType.Shared.DTOs.Requests.StatisticsGame;
using MonkeyType.Domain.Models;

namespace MonkeyType.Application.IServices
{
    public interface IStatisticsGameService
    {
        Task<IEnumerable<StatisticsGame>?> GetAllAsync();
        Task AddAsync(StatisticsGameRequestDTO statisticsGame);
        Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId);
        Task<StatisticsGame?> GetByIdAsync(Guid id);
        Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(string scope, int? durationInSeconds, string? mode, int topN);
    }
}